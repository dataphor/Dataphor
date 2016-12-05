import { APIService } from '../../shared/index';

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

`;
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
    }

    runScript() {
        this.oText = '';
        if (!this.running) {
            this.running = true;
            this._apiService
                .post(this.iTextTemp)
                .then(response => {
                    this.oText += '\n' + response;
                    this.running = false;
                })
                .catch(error => {
                    this.oText += '\n' + error;
                    this.running = false;
                    alert('Something went wrong:\n' + error);
                });
        }
    }

    clearOutput() {
        this.oText = '';
    }

}
