import { Component, Input, OnInit, OnDestroy } from '@angular/core';
import { FormGroup, FormControl } from '@angular/forms';
import { FormControlContainer } from '../form.component';

@Component({
    selector: 'd4-numerictextbox',
    template: require('./numerictextbox.component.html'),
    styles: [require('./numerictextbox.component.css')],
    providers: []
})
export class NumericTextboxComponent implements OnDestroy, OnInit{
    
    @Input() title: string = '';
    @Input() name: string = '';
    @Input() nilifblank: string = '';

    formGroup: FormGroup;
    formControl: FormControl;

    private group: string;
    private control: string;

    constructor(private _parent: FormControlContainer) {

        
    }

    ngOnInit() {
        // TODO: Change template so we don't have to do this every time
        let groupAndControl = this.name.split('.');
        this.group = groupAndControl[0];
        this.control = groupAndControl[1];

        //this.formGroup = new FormGroup({
        //    groupAndControl[0]: new FormControl()
        //});
        //this.formControl = groupAndControl[1];
        this.formControl = new FormControl(this.control, null);
        this._parent.addControl(this.control, this.formControl);
    }

    ngOnDestroy() {
        this._parent.removeControl(this.control);
    }


}
