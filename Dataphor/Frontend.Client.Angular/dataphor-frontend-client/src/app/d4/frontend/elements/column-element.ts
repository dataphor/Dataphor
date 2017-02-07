import { Input, OnInit, OnDestroy, ViewChildren } from '@angular/core';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';

import { IColumnElement } from '../interfaces';
import { InterfaceService } from '../interface';
import { NodeService } from '../nodes/index';

import { DataElement } from './index';

export class ColumnElement extends DataElement implements OnInit, IColumnElement {

    @Input('columnname') ColumnName: string = '';
    @Input('title') Title: string = '';
    
    constructor() {
        super();
    }

};