import { Input, OnInit, OnDestroy, ViewChildren } from '@angular/core';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';

import { ICheckBox } from '../interfaces';
import { InterfaceService } from '../interface';
import { NodeService } from '../nodes/index';
import { ColumnElement } from './index';

const DefaultWidth: number = 10;
const DefaultAutoUpdateInterval: number = 200;

export class CheckBox extends ColumnElement implements OnInit, ICheckBox {
    
    @Input('width') Width: number = 0;
    @Input('autoupdate') AutoUpdate: boolean;
    @Input('truefirst') TrueFirst: boolean;
    @Input('autoupdateinterval') AutoUpdateInterval: number = DefaultAutoUpdateInterval;

    constructor() {
        super();
    }

    ngOnInit() {
        
    }
    
};