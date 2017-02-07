import { Component, Input, OnInit, OnDestroy, ViewChildren } from '@angular/core';
import { IAction, IBlockAction } from '../../interfaces';
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
export class BlockActionComponent extends Action implements IBlockAction, OnInit, OnDestroy {

    constructor() {
        super();
    }


}
