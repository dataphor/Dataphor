import { Input, OnInit, OnDestroy, ViewChildren } from '@angular/core';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';

import { ITitledElement, TitleAlignment } from '../interfaces';
import { InterfaceService } from '../interface';
import { NodeService } from '../nodes/index';

import { ColumnElement } from './index';

export class TitledElement extends ColumnElement implements OnInit, ITitledElement {

    @Input('titlealignment') TitleAlignment: TitleAlignment = TitleAlignment.Top;
    
    constructor() {
        super();
    }

};