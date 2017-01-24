import { Input, OnInit, OnDestroy, ContentChildren, QueryList } from '@angular/core';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';

import { INotebook, IBaseNotebookPage, IAction } from '../interfaces';
import { InterfaceService } from '../interface';
import { NodeService } from '../nodes/index';
import { ControlContainer } from './index';

const DefaultWidth: number = 10;
const DefaultAutoUpdateInterval: number = 200;

export abstract class BaseNotebookPage extends ControlContainer implements OnInit {
    
    @Input('Title') Title: string = '';




    constructor() {
        super();
    }

    ngOnInit() {
        // TODO: set OnActivePageChange to the action referred to be _onActivePageChange
    }

    
};