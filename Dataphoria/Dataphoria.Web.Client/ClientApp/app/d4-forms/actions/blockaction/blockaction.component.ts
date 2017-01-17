import { Component, Input, OnInit, OnDestroy, ViewChildren } from '@angular/core';
import { IAction, INode } from '../../interfaces';
import { Action } from '../action';

import { NodeService } from '../../nodes';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';
//import { Observer } from 'rxjs/Observer';

import { KeyedCollection } from '../../system';

@Component({
    selector: 'd4-blockaction',
    template: require('./blockaction.component.html'),
    providers: []
})
export class BlockActionComponent extends Action implements OnInit, OnDestroy {
    
    @Input('text') Text: string;
    @Input('hint') Hint: string;
    @Input('image') Image: string;
    @Input('beforeexecute') BeforeExecute: string;
    private _beforeExecute: IAction;
    private _beforeExecuteLoaded: boolean = this.BeforeExecute ? false : true;
    @Input('afterexecute') AfterExecute: string;
    private _afterExecute: IAction;
    private _afterExecuteLoaded: boolean = this.AfterExecute ? false : true;
    @Input('visible') Visible: boolean;
    @Input('enabled') Enabled: boolean;
    
    // TODO: Figure out what to do with this 'User-Defined Scratchpad'
    @Input('userdata') UserData: Object;



    // Common

    //@ViewChildren(Node) private _children: KeyedCollection<INode>;
    private _parent: INode;
    //protected Owner: INode;
    

    constructor() {
        super();
    }

    

    

    

}
