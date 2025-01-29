// File filtering module for XDM browser extension

export class DownloadFilter {
    constructor() {
        this.filters = {
            fileTypes: new Set(),
            sizeRanges: [],
            customPatterns: []
        };
        this.loadFilters();
    }

    async loadFilters() {
        const stored = await chrome.storage.sync.get('downloadFilters');
        if (stored.downloadFilters) {
            this.filters = stored.downloadFilters;
        } else {
            // Set default filters
            this.filters.fileTypes = new Set([
                'mp4', 'mkv', 'webm',  // Video
                'mp3', 'wav', 'flac',  // Audio
                'pdf', 'doc', 'docx',  // Documents
                'zip', 'rar', '7z'     // Archives
            ]);
            this.filters.sizeRanges = [
                { min: 0, max: null }  // No size limit by default
            ];
            this.saveFilters();
        }
    }

    async saveFilters() {
        await chrome.storage.sync.set({
            downloadFilters: this.filters
        });
    }

    addFileType(extension) {
        this.filters.fileTypes.add(extension.toLowerCase());
        this.saveFilters();
    }

    removeFileType(extension) {
        this.filters.fileTypes.delete(extension.toLowerCase());
        this.saveFilters();
    }

    setSizeRange(min, max) {
        this.filters.sizeRanges = [{ min, max }];
        this.saveFilters();
    }

    addCustomPattern(pattern) {
        try {
            new RegExp(pattern); // Validate pattern
            this.filters.customPatterns.push(pattern);
            this.saveFilters();
            return true;
        } catch {
            return false;
        }
    }

    removeCustomPattern(pattern) {
        const index = this.filters.customPatterns.indexOf(pattern);
        if (index !== -1) {
            this.filters.customPatterns.splice(index, 1);
            this.saveFilters();
            return true;
        }
        return false;
    }

    shouldCapture(details) {
        const url = details.url.toLowerCase();
        const size = details.contentLength;

        // Check file type
        const extension = this.getFileExtension(url);
        if (extension && !this.filters.fileTypes.has(extension)) {
            return false;
        }

        // Check size ranges
        if (size !== -1 && this.filters.sizeRanges.length > 0) {
            const inRange = this.filters.sizeRanges.some(range => {
                const aboveMin = range.min === null || size >= range.min;
                const belowMax = range.max === null || size <= range.max;
                return aboveMin && belowMax;
            });
            if (!inRange) {
                return false;
            }
        }

        // Check custom patterns
        if (this.filters.customPatterns.length > 0) {
            const matchesPattern = this.filters.customPatterns.some(pattern => {
                try {
                    const regex = new RegExp(pattern);
                    return regex.test(url);
                } catch {
                    return false;
                }
            });
            if (!matchesPattern) {
                return false;
            }
        }

        return true;
    }

    getFileExtension(url) {
        try {
            const urlObj = new URL(url);
            const pathname = urlObj.pathname;
            const extension = pathname.split('.').pop().toLowerCase();
            return extension || null;
        } catch {
            return null;
        }
    }

    getFilters() {
        return {
            fileTypes: Array.from(this.filters.fileTypes),
            sizeRanges: this.filters.sizeRanges,
            customPatterns: this.filters.customPatterns
        };
    }
}

// Export singleton instance
export const downloadFilter = new DownloadFilter();
