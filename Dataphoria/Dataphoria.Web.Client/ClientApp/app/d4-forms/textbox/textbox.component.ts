import { Component, Input, OnInit, OnDestroy } from '@angular/core';
import { FormControl } from '@angular/forms';
import { FormControlContainer } from '../form.component';

@Component({
    selector: 'd4-textbox',
    template: require('./textbox.component.html'),
    styles: [require('./textbox.component.css')],
    providers: []
})
export class TextboxComponent implements OnInit, OnDestroy {

    @Input() name: string = '';
    @Input() title: string = '';
    @Input() maxlength: number;
    @Input() maxwidth: number;
    @Input() nilifblank: string = '';
    @Input() width: number;
    @Input() columnname: string = '';
    @Input() source: string = '';

    private controlId: string;
    private formControl: FormControl;

    constructor(private _form: FormControlContainer) { }

    ngOnInit() {
        let sourceAndControl = this.columnname.split('.');
        this.controlId = sourceAndControl[1];

        this.formControl = new FormControl(this.controlId, null);
        if (this.source) {
            this._form.addControl(this.controlId, this.formControl, this.source);
        }
    }

    ngOnDestroy() {
        if (this.source) {
            this._form.removeControl(this.controlId);
        }
    }

}