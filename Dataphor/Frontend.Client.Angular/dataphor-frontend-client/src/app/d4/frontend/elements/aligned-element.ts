import { Input, OnInit, OnDestroy, ViewChildren } from '@angular/core';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';

import { IAlignedElement, TextAlignment } from '../interfaces';
import { InterfaceService } from '../interface';
import { NodeService } from '../nodes/index';

import { TitledElement } from './index';

const DefaultWidth: number = 10;
const DefaultMaxWidth: number = 10;

export class AlignedElement extends TitledElement implements OnInit, IAlignedElement {

    @Input('textalignment') TextAlignment: TextAlignment = TextAlignment.Left;
    @Input('width') Width: number = DefaultWidth;
    @Input('maxwidth') MaxWidth: number = DefaultMaxWidth;
    
    constructor() {
        super();
    }

};