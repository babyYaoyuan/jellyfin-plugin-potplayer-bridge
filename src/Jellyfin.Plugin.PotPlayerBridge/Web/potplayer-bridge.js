(function () {
    'use strict';

    if (window.__potPlayerBridgeInstalled) {
        return;
    }
    window.__potPlayerBridgeInstalled = true;

    var buttonClass = 'potplayerBridgeButton';
    var ticketCache = new Map();
    var buildTimer = null;

    function log() {
        try {
            var args = Array.prototype.slice.call(arguments);
            args.unshift('[PotPlayerBridge]');
            console.info.apply(console, args);
        } catch (_) {
            // Logging must never break the page.
        }
    }

    function getApiClient() {
        try {
            if (window.ApiClient) {
                return window.ApiClient;
            }
            if (window.connectionManager && typeof window.connectionManager.currentApiClient === 'function') {
                return window.connectionManager.currentApiClient();
            }
        } catch (_) {
            return null;
        }
        return null;
    }

    function getDetailItemId() {
        var raw = String(window.location.hash || '').replace(/^#!/, '#');
        var question = raw.indexOf('?');
        var route = (question >= 0 ? raw.substring(1, question) : raw.substring(1)).toLowerCase();
        if (route !== '/details') {
            return null;
        }
        var params = new URLSearchParams(question >= 0 ? raw.substring(question + 1) : '');
        var id = params.get('id');
        return id && /^[0-9a-f]{32}$/i.test(id) ? id : null;
    }

    function isVisible(element) {
        return Boolean(element && element.isConnected && element.offsetParent !== null);
    }

    function getButtonContainer() {
        var containers = Array.from(document.querySelectorAll('.mainDetailButtons'));
        return containers.find(isVisible) || null;
    }

    function labels() {
        var chinese = String(navigator.language || '').toLowerCase().startsWith('zh');
        return chinese
            ? { ready: '使用 PotPlayer 播放', loading: '正在准备 PotPlayer 播放', failed: '准备失败，点击重试' }
            : { ready: 'Play with PotPlayer', loading: 'Preparing PotPlayer stream', failed: 'Preparation failed; click to retry' };
    }

    function ensureStyle() {
        if (document.getElementById('potplayerBridgeStyle')) {
            return;
        }
        var style = document.createElement('style');
        style.id = 'potplayerBridgeStyle';
        style.textContent =
            '.potplayerBridgeButton{text-decoration:none!important;color:inherit!important;}' +
            '.potplayerBridgeButton .detailButton-icon{color:#f4c542;}' +
            '.potplayerBridgeButton[aria-disabled="true"]{opacity:.45;cursor:wait;}' +
            '.potplayerBridgeButton.potplayerBridgeFailed{opacity:.75;cursor:pointer;}';
        document.head.appendChild(style);
    }

    function makeButton(itemId, container) {
        ensureStyle();
        var text = labels();
        var link = document.createElement('a');
        link.className = 'button-flat detailButton emby-button ' + buttonClass;
        link.href = '#';
        link.setAttribute('role', 'button');
        link.setAttribute('aria-disabled', 'true');
        link.setAttribute('aria-label', text.loading);
        link.setAttribute('title', text.loading);
        link.dataset.itemId = itemId;
        link.innerHTML =
            '<div class="detailButton-content">' +
            '<span class="material-icons detailButton-icon open_in_new" aria-hidden="true"></span>' +
            '</div>';
        link.addEventListener('click', function (event) {
            if (link.dataset.ready !== 'true') {
                event.preventDefault();
                prepareLink(itemId, link, true);
            }
        });

        var replayButton = container.querySelector('.btnReplay');
        var playButton = container.querySelector('.btnPlay');
        var anchor = replayButton || playButton;
        if (anchor && anchor.nextSibling) {
            container.insertBefore(link, anchor.nextSibling);
        } else {
            container.appendChild(link);
        }
        return link;
    }

    function setLoading(link) {
        var text = labels();
        link.dataset.ready = 'false';
        link.href = '#';
        link.classList.remove('potplayerBridgeFailed');
        link.setAttribute('aria-disabled', 'true');
        link.setAttribute('aria-label', text.loading);
        link.setAttribute('title', text.loading);
    }

    function setReady(link, launchUrl) {
        var text = labels();
        link.dataset.ready = 'true';
        link.href = launchUrl;
        link.classList.remove('potplayerBridgeFailed');
        link.removeAttribute('aria-disabled');
        link.setAttribute('aria-label', text.ready);
        link.setAttribute('title', text.ready);
    }

    function setFailed(link) {
        var text = labels();
        link.dataset.ready = 'false';
        link.href = '#';
        link.classList.add('potplayerBridgeFailed');
        link.setAttribute('aria-disabled', 'false');
        link.setAttribute('aria-label', text.failed);
        link.setAttribute('title', text.failed);
    }

    function prepareLink(itemId, link, force) {
        var cached = ticketCache.get(itemId);
        if (!force && cached && cached.expiresAt > Date.now() + 60000) {
            setReady(link, cached.launchUrl);
            return;
        }

        var apiClient = getApiClient();
        if (!apiClient || typeof apiClient.fetch !== 'function') {
            setTimeout(function () {
                if (link.isConnected && link.dataset.itemId === getDetailItemId()) {
                    prepareLink(itemId, link, force);
                }
            }, 300);
            return;
        }

        setLoading(link);
        apiClient.fetch({
            url: apiClient.getUrl('Plugins/PotPlayerBridge/Tickets/' + itemId),
            type: 'POST',
            dataType: 'json'
        }).then(function (response) {
            if (!response) {
                throw new Error('Empty ticket response');
            }
            var streamPath = response.StreamPath || response.streamPath;
            var expiresRaw = response.ExpiresAtUtc || response.expiresAtUtc;
            if (!streamPath) {
                throw new Error('Ticket response has no stream path');
            }
            var streamUrl = new URL(streamPath, window.location.origin).href;
            // Use an opaque custom-protocol URL. Chromium 130+ normalizes
            // potplayer://http://... to potplayer://http//... and drops the
            // nested scheme's colon; potplayer:http://... is preserved.
            var launchUrl = 'potplayer:' + streamUrl;
            var expiresAt = Date.parse(expiresRaw || '') || (Date.now() + 5 * 60 * 1000);
            ticketCache.set(itemId, { launchUrl: launchUrl, expiresAt: expiresAt });
            if (link.isConnected && link.dataset.itemId === getDetailItemId()) {
                setReady(link, launchUrl);
                log('ready for item', itemId);
            }
        }).catch(function (error) {
            console.warn('[PotPlayerBridge] ticket request failed', error);
            if (link.isConnected && link.dataset.itemId === getDetailItemId()) {
                setFailed(link);
            }
        });
    }

    function buildButton() {
        var itemId = getDetailItemId();
        var existing = document.querySelector('.' + buttonClass);
        if (!itemId) {
            if (existing) {
                existing.remove();
            }
            return;
        }

        var container = getButtonContainer();
        if (!container) {
            return;
        }

        if (existing && existing.dataset.itemId !== itemId) {
            existing.remove();
            existing = null;
        }
        if (existing && existing.parentElement !== container) {
            existing.remove();
            existing = null;
        }

        var link = existing || makeButton(itemId, container);
        if (link.dataset.ready !== 'true') {
            prepareLink(itemId, link, false);
        }
    }

    function scheduleBuild() {
        clearTimeout(buildTimer);
        buildTimer = setTimeout(buildButton, 80);
    }

    window.addEventListener('hashchange', scheduleBuild);
    new MutationObserver(scheduleBuild).observe(document.body, { childList: true, subtree: true });
    scheduleBuild();
    log('client integration installed');
}());
