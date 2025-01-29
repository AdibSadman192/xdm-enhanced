"use strict";
import App from './app.js';
import { initRequestWatcher } from './request-watcher.js';
import { initLogger } from './logger.js';
import { initConnector } from './connector.js';

// Initialize core components
const logger = initLogger();
const connector = initConnector();
const watcher = initRequestWatcher(connector, logger);

// Enhanced video detection
const videoPatterns = {
    youtube: {
        pattern: /^https?:\/\/(www\.)?(youtube\.com|youtu\.be)\/.+/i,
        shorts: /^https?:\/\/(www\.)?youtube\.com\/shorts\/.+/i
    },
    vimeo: /^https?:\/\/(www\.)?vimeo\.com\/.+/i,
    dailymotion: /^https?:\/\/(www\.)?dailymotion\.com\/.+/i,
    // Add more platforms as needed
};

// Stream quality options
const streamQualities = {
    auto: 'Auto',
    '4k': '2160p',
    '2k': '1440p',
    'fullhd': '1080p',
    'hd': '720p',
    'sd': '480p'
};

// Enhanced media type detection
const mediaTypes = {
    video: /^video\//i,
    audio: /^audio\//i,
    stream: /^application\/(x-mpegURL|vnd\.apple\.mpegURL|dash\+xml)/i
};

// Improved request watcher
chrome.webRequest.onBeforeRequest.addListener(
    async function(details) {
        if (details.type === 'media' || isStreamingUrl(details.url)) {
            const videoInfo = await analyzeVideoStream(details);
            if (videoInfo.isValidStream) {
                notifyXDM(videoInfo);
            }
        }
    },
    { urls: ["<all_urls>"] },
    ["requestBody"]
);

// Enhanced video stream analysis
async function analyzeVideoStream(details) {
    const url = details.url;
    const videoInfo = {
        url: url,
        isValidStream: false,
        platform: detectPlatform(url),
        quality: null,
        format: null,
        isAdaptive: false
    };

    if (videoInfo.platform) {
        videoInfo.isValidStream = true;
        videoInfo.quality = await detectQuality(url);
        videoInfo.format = detectFormat(details.type);
        videoInfo.isAdaptive = isAdaptiveStream(details);
    }

    return videoInfo;
}

function detectPlatform(url) {
    for (const [platform, pattern] of Object.entries(videoPatterns)) {
        if (typeof pattern === 'object') {
            // Handle multiple patterns per platform
            for (const [type, regex] of Object.entries(pattern)) {
                if (regex.test(url)) return { name: platform, type };
            }
        } else if (pattern.test(url)) {
            return { name: platform, type: 'default' };
        }
    }
    return null;
}

async function detectQuality(url) {
    // Implement quality detection logic based on platform
    // This is a placeholder - actual implementation would need platform-specific logic
    return 'auto';
}

function detectFormat(contentType) {
    if (mediaTypes.stream.test(contentType)) {
        return 'adaptive';
    } else if (mediaTypes.video.test(contentType)) {
        return 'video';
    } else if (mediaTypes.audio.test(contentType)) {
        return 'audio';
    }
    return 'unknown';
}

function isAdaptiveStream(details) {
    return details.type === 'media' && 
           (details.url.includes('.m3u8') || 
            details.url.includes('.mpd') || 
            mediaTypes.stream.test(details.type));
}

function isStreamingUrl(url) {
    return url.includes('.m3u8') || 
           url.includes('.mpd') || 
           Object.values(videoPatterns).some(pattern => 
               pattern instanceof RegExp ? pattern.test(url) : 
               Object.values(pattern).some(p => p.test(url))
           );
}

// Notify XDM of detected video
function notifyXDM(videoInfo) {
    chrome.runtime.sendNativeMessage('com.xdm.native', {
        type: 'video_detected',
        data: videoInfo
    }, response => {
        if (chrome.runtime.lastError) {
            console.error('Error notifying XDM:', chrome.runtime.lastError);
            return;
        }
        console.log('XDM notified successfully:', response);
    });
}

// Handle installation
chrome.runtime.onInstalled.addListener(async (details) => {
    if (details.reason === 'install' || details.reason === 'update') {
        // Initialize storage with default settings
        await chrome.storage.local.set({
            enabled: true,
            videoDetection: true,
            fileFilters: {
                enabled: true,
                types: ['video', 'audio', 'archive', 'document'],
                minSize: 1024 * 1024, // 1MB
                customPatterns: []
            },
            browserType: detectBrowserType()
        });

        // Create context menu items
        createContextMenus();
    }
});

// Detect browser type
function detectBrowserType() {
    const userAgent = navigator.userAgent;
    if (userAgent.includes('Edg/')) return 'edge';
    if (userAgent.includes('Brave/')) return 'brave';
    return 'chrome';
}

// Create context menus
function createContextMenus() {
    chrome.contextMenus.create({
        id: 'downloadWithXDM',
        title: 'Download with XDM',
        contexts: ['link', 'video', 'audio']
    });

    chrome.contextMenus.create({
        id: 'captureVideo',
        title: 'Capture Video',
        contexts: ['page']
    });

    chrome.contextMenus.create({
        id: 'xdm-download',
        title: 'Download with XDM',
        contexts: ['link', 'video', 'audio']
    });
}

// Handle context menu clicks
chrome.contextMenus.onClicked.addListener((info, tab) => {
    if (info.menuItemId === 'downloadWithXDM') {
        connector.sendDownloadRequest({
            url: info.linkUrl || info.srcUrl,
            referrer: tab.url,
            filename: getFileNameFromUrl(info.linkUrl || info.srcUrl)
        });
    } else if (info.menuItemId === 'captureVideo') {
        watcher.startVideoCapture(tab.id);
    } else if (info.menuItemId === 'xdm-download') {
        const url = info.linkUrl || info.srcUrl;
        if (url) {
            chrome.runtime.sendNativeMessage('com.xdm.native', {
                type: 'download_request',
                data: {
                    url: url,
                    referrer: tab.url,
                    filename: getFilenameFromUrl(url)
                }
            });
        }
    }
});

// Helper function to get filename from URL
function getFileNameFromUrl(url) {
    try {
        const urlObj = new URL(url);
        const pathname = urlObj.pathname;
        return pathname.substring(pathname.lastIndexOf('/') + 1);
    } catch {
        return '';
    }
}

function getFilenameFromUrl(url) {
    try {
        const urlObj = new URL(url);
        const pathname = urlObj.pathname;
        return pathname.substring(pathname.lastIndexOf('/') + 1) || 'download';
    } catch {
        return 'download';
    }
}

// Export for testing
export const __testing = {
    detectBrowserType,
    getFileNameFromUrl
};

const app = new App();
app.start();
