import { Injectable } from '@angular/core';

@Injectable()
export class UtilityService {

    // Creates psuedo-DOM into which we place our raw string
    // Once placed in DOM, JS unescapes any escaped characters
    private decodeHTML(input) {
    var doc = new DOMParser().parseFromString(input, "text/html");
    return doc.documentElement.textContent;
    }

    decodeInline(source: string): string {
        return this.decodeHTML(source);
    }

    // Takes object with source-values combined (e.g., Main.ID in { 'Main.ID': '001' }) and returns the values, discarding the sources
    getValueFromSourceValue(original: Object): Object {
        let keys = Object.keys(original);
        let cleaned = new Object();
        for (let key of keys) {
            cleaned[key.split('.')[1]] = original[key];
        }
        return cleaned;
    }

}