import { ActionService } from '../actions/index';
import { Injectable, OnDestroy } from '@angular/core';

@Injectable()
export class InterfaceService {


    private _actionService: ActionService;
    get ActionService(): ActionService {
        return this._actionService;
    }
    set ActionService(value: ActionService) {
        this._actionService = value;
    }

    constructor() {
        this._actionService = new ActionService();
    }


}