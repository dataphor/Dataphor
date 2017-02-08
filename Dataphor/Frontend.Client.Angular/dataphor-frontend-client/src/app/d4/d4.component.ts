import { Component, OnInit, ViewChildren, QueryList, OnDestroy, forwardRef, Injector } from '@angular/core';
import { AbstractControl, FormControl, FormGroup, ControlContainer, Validators, FormGroupDirective } from '@angular/forms';
import { ISource } from './interfaces';
import { D4 } from './d4';
import { D4Service } from './d4.service';


@Component({
    selector: 'd4-form',
    template: `<ng-content></ng-content>`,
    providers: []
})
export class D4Component extends D4 {

    constructor(private _d4Service: D4Service) {
        super();
    }

}