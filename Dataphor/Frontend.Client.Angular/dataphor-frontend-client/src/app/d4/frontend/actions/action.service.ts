import { Node } from '../node';
import { INode, IAction, ILayoutDisableable, IBlockable } from '../interfaces/index';
import { KeyedCollection } from '../system';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';
import { Injectable, OnDestroy } from '@angular/core';

@Injectable()
export class ActionService implements OnDestroy {

    private actionDictionary: KeyedCollection<IAction>;
    private _actionDictionarySubject$: BehaviorSubject<KeyedCollection<IAction>>;

    constructor() {
        this.actionDictionary = new KeyedCollection<IAction>();
        this._actionDictionarySubject$ = new BehaviorSubject<KeyedCollection<IAction>>(null);
    }


    GetActionByName(actionName: string): IAction {
        return this.actionDictionary.Item(actionName);
    }

    GetAllActions(): KeyedCollection<IAction> {
        return this.actionDictionary;
    }

    AddAction(action: IAction): void {
        this.actionDictionary.Add(action.Name, action);
        this.NotifySubscribers();
    }

    GetActionDictionarySubject(): BehaviorSubject<KeyedCollection<IAction>> {
        return this._actionDictionarySubject$;
    }

    NotifySubscribers(): void {
        this._actionDictionarySubject$.next(this.actionDictionary);
    }

    ngOnDestroy() {
        this._actionDictionarySubject$.unsubscribe();
    }

}
