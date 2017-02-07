import { Component, OnInit, ViewChildren, QueryList, OnDestroy, forwardRef, Injector } from '@angular/core';
import { AbstractControl, FormControl, FormGroup, ControlContainer, Validators, FormGroupDirective } from '@angular/forms';
import { ISource } from './interfaces';
import { D4Service } from './d4.service';


@Component({
    selector: 'd4-form',
    template: `
    <form [formGroup]="form" novalidate>
        <ng-content></ng-content>
        <div>
            <button type="button" (click)="onSubmit()">Submit</button>
        </div>
    </form>
  `,
    providers: [formGroupContainerProvider]
})
export class D4Component extends D4 {

    form = new FormGroup({});

    constructor(private _formService: D4Service) {
        super();
    }



}