import { Component, Input, OnInit, OnDestroy, ViewChildren } from '@angular/core';
import { IAction, IBlockAction } from '../../interfaces';
import { BlockAction } from './';

import { NodeService } from '../../nodes';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';

import { KeyedCollection } from '../../..';

@Component({
    selector: 'd4-blockaction',
    template: require('./blockaction.component.html'),
    providers: []
})
export class BlockActionComponent extends BlockAction implements IBlockAction, OnInit, OnDestroy {

    constructor() {
        super();
    }

    ngOnInit() {

    }

    ngOnDestroy() {
        
    }


}
