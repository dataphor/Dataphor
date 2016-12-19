import { Injectable } from '@angular/core';

@Injectable()
export class UtilityService {


    private decodeHTML(input) {
    var doc = new DOMParser().parseFromString(input, "text/html");
    return doc.documentElement.textContent;
}

    decodeInline(source: string): string {
        let decoded = this.decodeHTML(source);
        decoded = this.decodeHTML(decoded);
        return decoded;
    }

}