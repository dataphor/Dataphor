import { Component, Input, OnInit } from '@angular/core';
import { FormGroup, FormControl, AbstractControl, Validators } from '@angular/forms';
import { FormControlContainer } from '../form.component';
import { APIService, IResponse, ResponseSingle, ResponseSet, UtilityService } from '../../shared/index';

@Component({
    selector: 'd4-source',
    template: require('./source.component.html'),
    styles: [require('./source.component.css')],
    providers: []
})
export class SourceComponent implements OnInit {
    
    @Input() name: string = '';
    @Input() expression: string = '';

    formGroup: AbstractControl;

    constructor(private _apiService: APIService, private _utilityService: UtilityService, private _form: FormControlContainer) { }

    ngOnInit() {
        this.formGroup = this._form.addSource(this.name, new FormGroup({}));

        let decodedSource = this._utilityService.decodeInline(this.expression);

        let expression = 'select ' + this.expression;
        this._apiService
            .post({ value: this.expression })
            .then(response => {
                let handledResponse = this._apiService.handleResponse(response);
                console.log(handledResponse);
                if (Array.isArray(handledResponse)) {
                    // Return first value from array of objects
                    this._form.updateGroup(this.name, this.formGroup, handledResponse[0].value);
                }
                else {
                    this._form.updateGroup(this.name, this.formGroup, handledResponse.value);
                }
            })
            .catch(error => {
                var result = error.status + ': ' + error.statusText;
                result += '\r\n';
                result += error.responseText;
                console.log(result);
            });
    }
    
}