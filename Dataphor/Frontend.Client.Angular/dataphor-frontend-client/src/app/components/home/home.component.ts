import { APIService, IResponse, ResponseSingle, ResponseSet } from '../../shared/index';

import { Component } from '@angular/core';

import 'brace/theme/clouds';
import 'brace/mode/sql';

@Component({
    selector: 'd4-home',
    template: require('./home.component.html'),
    styles: [require('./home.component.css')],
    providers: [APIService]
})
export class HomeComponent {

    error = '';
    running = false;
    model = {
        query: ''
    };

    // Input editor settings
    iText = `// Enter your D4 script here and press 'Run'
// The results will be displayed on the console to the right
select `;
    // Carries the latest value from input events
    iTextTemp: string;
    iOptions: any = { vScrollBarAlwaysVisible: true };
    iTheme = 'clouds';

    oText = `// D4 expression results
`;
    oOptions: any = { vScrollBarAlwaysVisible: true, showLineNumbers: false, showGutter: false };
    oIsReadOnly = true;

    constructor(private _apiService: APIService) { }

    // Stores last known value of text input window
    onInputChange(code) {
        this.iTextTemp = code;
    };

    runScript() {
        this.oText = '';
        if (!this.running) {
            this.running = true;
            this._apiService
                .post({ value: this.iTextTemp })
                .then(response => {
                    const handledResponse = this._apiService.handleResponse(response);
                    this.oText += handledResponse + '\r\n';
                    this.running = false;
                })
                .catch(error => {
                    let result = error.status + ': ' + error.statusText;
                    result += '\r\n';
                    result += error.responseText;
                    this.oText += result;
                    this.running = false;
                });
        }
    };

    clearOutput() {
        this.oText = '';
    }

}
