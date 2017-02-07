import { Input, OnInit, OnDestroy, ViewChildren } from '@angular/core';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';

import { IChoice } from '../interfaces';
import { InterfaceService } from '../interface';
import { NodeService } from '../nodes/index';
import { ColumnElement } from './index';

const DefaultAutoUpdateInterval: number = 200;

export class Choice extends ColumnElement implements OnInit, IChoice {
    
    @Input('width') Width: number = 0;
    @Input('items') Items: string = '';
    // { name, value }
    ItemsList: Array<Object>;
    @Input('autoupdate') AutoUpdate: boolean;
    @Input('autoupdateinterval') AutoUpdateInterval: number = DefaultAutoUpdateInterval;
    @Input('columns') Columns: number;

    constructor() {
        super();
    }

    ngOnInit() {
        this.ItemsList = this.SplitItems();
    }

    // Ex:
    // Triangle=Triangle,\r\nCircle=Circle
    private SplitItems(): Array<Object> {
        let result = new Array<Object>();
        let namesWithValues = this.Items.split(/\r|\n/);
        for (let i = 0; i < namesWithValues.length; i++) {
            let nameThenValue = namesWithValues[i].split('=');
            //if (nameThenValue.length !== 2) {
            //    throw new ClientException(ClientException.Codes.InvalidChoiceItems, Name);
            //}
            result.push({ name: nameThenValue[0].trim(), value: nameThenValue[1].trim() });
        }
        return result;
    }


};