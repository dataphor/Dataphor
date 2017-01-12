import { Component, Input } from '@angular/core';
import { IAction } from '../../interfaces';
import { Action } from '../action';
import { InterfaceService } from '../../interface';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';
import { Observer } from 'rxjs/Observer';
import { KeyedCollection } from '../../system';

@Component({
    selector: 'd4-blockaction',
    template: require('./blockaction.component.html'),
    providers: []
})
export class BlockActionComponent {
    
    @Input('text') Text: string;
    @Input('hint') Hint: string;
    @Input('image') Image: string;
    @Input('beforeexecute') BeforeExecute: string;
    private _beforeExecute: IAction;
    // TODO: Attach to Observable Action so we can guarantee we point to the right action
    // set
    @Input('afterexecute') AfterExecute: string;
    private _afterExecute: IAction;
    @Input('visible') Visible: boolean;
    @Input('enabled') Enabled: boolean;
    @Input('name') Name: string;
    // TODO: Figure out what to do with this 'User-Defined Scratchpad'
    @Input('userdata') UserData: Object;

    private _loadedExternals: boolean = false;
    private _actionDictionaryObserver: Observer<KeyedCollection<IAction>>;

    constructor(private _interfaceService: InterfaceService) {
        this._actionDictionaryObserver = this._interfaceService.ActionService.GetActionDictionarySubject().subscribe({
            next: (x) => { console.log('Enabled changed to ${ x }') }
        });
    }



    // check if referenced Actions have returned
    CheckExternals(): void {
        if (!this._loadedExternals) {
            if (this._afterExecute && this._beforeExecute) {
                this._loadedExternals = true;
            }
        }
    }

}
