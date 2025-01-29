// Simple logger module
export function initLogger() {
    const LOG_LEVELS = {
        DEBUG: 0,
        INFO: 1,
        WARN: 2,
        ERROR: 3
    };

    let currentLevel = LOG_LEVELS.INFO;

    // Load log level from storage
    chrome.storage.local.get(['logLevel'], (result) => {
        if (result.logLevel && LOG_LEVELS[result.logLevel] !== undefined) {
            currentLevel = LOG_LEVELS[result.logLevel];
        }
    });

    function formatMessage(level, ...args) {
        const timestamp = new Date().toISOString();
        const prefix = `[XDM][${level}][${timestamp}]`;
        return [prefix, ...args];
    }

    function debug(...args) {
        if (currentLevel <= LOG_LEVELS.DEBUG) {
            console.debug(...formatMessage('DEBUG', ...args));
        }
    }

    function log(...args) {
        if (currentLevel <= LOG_LEVELS.INFO) {
            console.log(...formatMessage('INFO', ...args));
        }
    }

    function warn(...args) {
        if (currentLevel <= LOG_LEVELS.WARN) {
            console.warn(...formatMessage('WARN', ...args));
        }
    }

    function error(...args) {
        if (currentLevel <= LOG_LEVELS.ERROR) {
            console.error(...formatMessage('ERROR', ...args));
        }
    }

    function setLogLevel(level) {
        if (LOG_LEVELS[level] !== undefined) {
            currentLevel = LOG_LEVELS[level];
            chrome.storage.local.set({ logLevel: level });
        }
    }

    // Public API
    return {
        debug,
        log,
        warn,
        error,
        setLogLevel
    };
}