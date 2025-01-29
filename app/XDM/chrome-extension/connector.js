"use strict";
import Logger from './logger.js';

const APP_BASE_URL = "http://127.0.0.1:8597";

// Enhanced connector with cross-browser support
export function initConnector() {
    const NATIVE_HOST = {
        chrome: 'com.xdm.native',
        edge: 'com.xdm.native.edge',
        brave: 'com.xdm.native.brave'
    };

    let port = null;
    let browserType = 'chrome';
    let reconnectAttempts = 0;
    const MAX_RECONNECT_ATTEMPTS = 3;

    // Load browser type from storage
    chrome.storage.local.get(['browserType'], (result) => {
        if (result.browserType) {
            browserType = result.browserType;
        }
    });

    function connect() {
        try {
            port = chrome.runtime.connectNative(NATIVE_HOST[browserType]);
            
            port.onMessage.addListener((message) => {
                console.log('Received from native app:', message);
            });

            port.onDisconnect.addListener(() => {
                const error = chrome.runtime.lastError;
                console.error('Disconnected from native app:', error);
                port = null;

                if (reconnectAttempts < MAX_RECONNECT_ATTEMPTS) {
                    reconnectAttempts++;
                    setTimeout(connect, 1000 * reconnectAttempts);
                }
            });

            reconnectAttempts = 0;
        } catch (error) {
            console.error('Failed to connect to native app:', error);
        }
    }

    function sendMessage(message) {
        if (!port) {
            connect();
        }

        try {
            port.postMessage(message);
            return true;
        } catch (error) {
            console.error('Failed to send message:', error);
            return false;
        }
    }

    function sendDownloadRequest(request) {
        return sendMessage({
            type: 'download',
            data: {
                ...request,
                timestamp: Date.now(),
                browser: browserType
            }
        });
    }

    function sendStreamDetected(stream) {
        return sendMessage({
            type: 'stream',
            data: {
                ...stream,
                timestamp: Date.now(),
                browser: browserType
            }
        });
    }

    // Connect on initialization
    connect();

    // Public API
    return {
        sendDownloadRequest,
        sendStreamDetected
    };
}