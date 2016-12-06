import { APIService, IResponse, ResponseSingle, ResponseSet } from '../../shared/index';

import { Component } from '@angular/core';

import 'brace/theme/clouds';
import 'brace/mode/sql';

@Component({
    selector: 'home',
    template: require('./home.component.html'),
    styles: [require('./home.component.css')],
    providers: [APIService]
})
export class HomeComponent {

    constructor(private _apiService: APIService) { }

    error: string = '';
    running: boolean = false;
    model = {
        query: ''
    };

    // Input editor settings
    iText: string = `// Enter your D4 script here and press 'Run'
// The results will be displayed on the console to the right
select `;
    // Carries the latest value from input events
    iTextTemp: string;
    iOptions: any = { vScrollBarAlwaysVisible: true };
    iTheme: string = "clouds";

    oText: string = `// D4 expression results
`;
    oOptions: any = { vScrollBarAlwaysVisible: true, showLineNumbers: false, showGutter: false };
    oIsReadOnly: boolean = true;

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
                    let handledResponse = this.handleResponse(response);
                    this.oText += handledResponse + '\r\n';
                    this.running = false;
                })
                .catch(error => {
                    var result = error.status + ': ' + error.statusText;
                    result += '\r\n';
                    result += error.responseText;
                    this.oText += result;
                    this.running = false;
                });
        }
    };

    // TODO: Move to service
    handleResponse(response: any): any {
        let handledResponse = '';
        let responseModel: IResponse; 
        if (Array.isArray(response)) {
            responseModel = new ResponseSet(response);
            for (let r of response) {
                handledResponse += JSON.stringify(r) + '\r\n';
            }
        }
        else {
            responseModel = new ResponseSingle(response);
            handledResponse += JSON.stringify(responseModel) + '\r\n';
        }
        return handledResponse;
    };

    clearOutput() {
        this.oText = '';
    }

}
