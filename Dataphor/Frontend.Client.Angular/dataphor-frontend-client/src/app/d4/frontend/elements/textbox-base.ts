import { Input, OnInit, OnDestroy, ViewChildren } from '@angular/core';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';

import { ITextBoxBase } from '../interfaces';
import { InterfaceService } from '../interface';
import { NodeService } from '../nodes/index';

import { AlignedElement } from './index';

export class TextboxBase extends AlignedElement implements OnInit, ITextBoxBase {

    @Input('nilifblank') NilIfBlank: boolean = false;
    
    constructor() {
        super();
    }

};