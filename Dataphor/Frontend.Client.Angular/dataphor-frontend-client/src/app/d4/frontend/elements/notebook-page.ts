import { Input, OnInit, OnDestroy, ContentChildren, QueryList } from '@angular/core';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';

import { IFrame, INotebookFramePage } from '../interfaces';
import { InterfaceService } from '../interface';
import { NodeService } from '../nodes/index';
import { BaseNotebookPage } from './index';

const DefaultWidth: number = 10;
const DefaultAutoUpdateInterval: number = 200;

export class NotebookPage extends BaseNotebookPage implements OnInit, IFrame, INotebookFramePage {
    

    constructor() {
        super();
    }

    ngOnInit() {
        // TODO: set OnActivePageChange to the action referred to be _onActivePageChange
    }

    
};