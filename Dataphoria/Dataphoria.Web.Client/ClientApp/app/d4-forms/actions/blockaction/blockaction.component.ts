import { Component, Input } from '@angular/core';
import { IAction } from '../../interfaces';
import { Action, ActionService } from '../action';

@Component({
    selector: 'd4-blockaction',
    template: require('./blockaction.component.html'),
    providers: [ActionService]
})
export class BlockActionComponent {
    
    @Input('text') Text: string;
    @Input('hint') Hint: string;
    @Input('image') Image: string;
    @Input('beforeexecute') BeforeExecute: string;
    _beforeExecute: IAction;
    // TODO: Attach to Observable Action so we can guarantee we point to the right action

    constructor(private _actionService: ActionService) {
        
    }

}
