import { downloadFilter } from './filters.js';
import { logger } from './logger.js';

export class RequestWatcher {
    constructor(connector) {
        this.connector = connector;
        this.activeCaptures = new Map();
        this.initializeWatcher();
    }

    initializeWatcher() {
        chrome.webRequest.onBeforeRequest.addListener(
            details => this.handleRequest(details),
            { urls: ["<all_urls>"] },
            ["requestBody"]
        );

        chrome.webRequest.onHeadersReceived.addListener(
            details => this.handleHeaders(details),
            { urls: ["<all_urls>"] },
            ["responseHeaders"]
        );
    }

    async handleRequest(details) {
        try {
            // Skip if not a downloadable request
            if (!this.isDownloadableRequest(details)) {
                return;
            }

            // Apply filters
            if (!downloadFilter.shouldCapture(details)) {
                logger.debug('Request filtered out:', details.url);
                return;
            }

            // Process the request
            await this.processRequest(details);
        } catch (error) {
            logger.error('Error handling request:', error);
        }
    }

    async handleHeaders(details) {
        try {
            if (this.activeCaptures.has(details.requestId)) {
                const headers = this.extractHeaders(details.responseHeaders);
                await this.connector.sendHeaders(details.requestId, headers);
            }
        } catch (error) {
            logger.error('Error handling headers:', error);
        }
    }

    isDownloadableRequest(details) {
        // Skip main_frame requests
        if (details.type === 'main_frame') {
            return false;
        }

        // Check if it's a media type
        if (details.type === 'media') {
            return true;
        }

        // Check URL patterns
        const url = details.url.toLowerCase();
        if (url.includes('video') || url.includes('audio') || 
            url.includes('stream') || url.includes('download')) {
            return true;
        }

        return false;
    }

    async processRequest(details) {
        const requestInfo = {
            id: details.requestId,
            url: details.url,
            method: details.method,
            type: details.type,
            timestamp: Date.now(),
            tabId: details.tabId,
            size: -1 // Will be updated when headers are received
        };

        // Add to active captures
        this.activeCaptures.set(details.requestId, new Set());

        // Send to connector
        await this.connector.sendRequest(requestInfo);
    }

    extractHeaders(headers) {
        const result = {};
        if (Array.isArray(headers)) {
            for (const header of headers) {
                result[header.name.toLowerCase()] = header.value;
            }
        }
        return result;
    }

    startVideoCapture(tabId) {
        chrome.tabs.sendMessage(tabId, {
            action: 'startVideoCapture'
        });
        this.activeCaptures.set(tabId, new Set());
        logger.log('Started video capture for tab:', tabId);
    }

    stopVideoCapture(tabId) {
        chrome.tabs.sendMessage(tabId, {
            action: 'stopVideoCapture'
        });
        this.activeCaptures.delete(tabId);
        logger.log('Stopped video capture for tab:', tabId);
    }
}

export function initRequestWatcher(connector) {
    return new RequestWatcher(connector);
}