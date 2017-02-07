import { Input, OnInit, OnDestroy, ContentChildren, QueryList } from '@angular/core';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';

import { INotebook, IBaseNotebookPage, IAction } from '../interfaces';
import { InterfaceService } from '../interface';
import { NodeService } from '../nodes/index';
import { ControlContainer, NotebookPage } from './index';

const DefaultWidth: number = 10;
const DefaultAutoUpdateInterval: number = 200;

export class Notebook extends ControlContainer implements OnInit, INotebook {
    
    @Input('activepage') ActivePage: IBaseNotebookPage;
    @Input('onactivepagechange') _onActivePageChange: string;
    OnActivePageChange: IAction = null;

    @ContentChildren('d4-notebookpage') NotebookPages: QueryList<NotebookPage>;

    SetActive(page: IBaseNotebookPage): void {
        if (this.ActivePage !== page) {
            let oldPage: IBaseNotebookPage = this.ActivePage;
            this.ActivePage = page;
        }
    }

    private IsChildNode(node: INode): boolean {
        // TODO: Wire up to service
        return false;
    }

    constructor() {
        super();
    }

    ngOnInit() {
        // TODO: set OnActivePageChange to the action referred to be _onActivePageChange
    }

    
};