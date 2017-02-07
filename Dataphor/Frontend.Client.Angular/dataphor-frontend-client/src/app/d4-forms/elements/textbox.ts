import { Input, OnInit, OnDestroy, ViewChildren } from '@angular/core';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';

import { ITextBox } from '../interfaces';
import { InterfaceService } from '../interface';
import { NodeService } from '../nodes/index';
import { TextboxBase } from './index';

export class Textbox extends TextboxBase implements OnInit, ITextBox {

    @Input('acceptsreturn') AcceptsReturn: boolean = true;
    @Input('acceptstabs') AcceptsTabs: boolean = false;
    @Input('height') Height: number = 1;
    @Input('ispassword') IsPassword: boolean = false;
    @Input('maxlength') MaxLength: number = -1;
    @Input('wordwrap') WordWrap: boolean = false;

    constructor() {
        super();
    }

};