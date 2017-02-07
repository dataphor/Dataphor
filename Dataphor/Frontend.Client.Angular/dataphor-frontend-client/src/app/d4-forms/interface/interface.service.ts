import { ActionService } from '../actions/index';
import { NodeService } from '../nodes/index';
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

    private _nodeService: NodeService;
    get NodeService(): NodeService {
        return this._nodeService;
    }
    set NodeService(value: NodeService) {
        this._NodeService = value;
    }


    constructor() {
        this._actionService = new ActionService();
        this._nodeService = new NodeService();
    }


}