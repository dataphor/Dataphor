import { Input, OnInit, OnDestroy } from '@angular/core';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';

import { IDataElement, ISource } from '../interfaces';
import { InterfaceService } from '../interface';
import { NodeService } from '../nodes/index';

import { Element } from './index';

export abstract class DataElement extends Element implements OnInit, OnDestroy, IDataElement {

    constructor() {
        super();
    }

    ngOnInit() {
        this.SetSource(this._source);
    }


    // string for the source (not the actual source)
    @Input('source') private _source: string;

    get Source(): ISource {
        return this.Source;
    }

    SetSource(source: string) {
        //TODO: Get source from service, given string
    }

    //protected abstract InternalUpdateSource(): void;

    @Input('readonly') _readOnly: boolean = false;

    get ReadOnly(): boolean {
        return this._readOnly;
    }
    set ReadOnly(value: boolean) {
        if (this._readOnly !== value) {
            this._readOnly = value;
            if (this.Active) {
                this.UpdateReadOnly();
            }
        }
    }

    private UpdateReadOnly(): void {
        //this.InternalUpdateReadOnly();
    }

    //protected abstract InternalUpdateReadOnly(): void;

    GetReadOnly(): boolean {
        return this._readOnly;
    }

    GetTabStop(): boolean {
        return super.GetTabStop() && !this.GetReadOnly();
    }

    protected Activate(): void {
        //this.InternalUpdateReadOnly();
        super.Activate();
        //this.InternalUpdateSource();
    }

    ngOnDestroy() {

    }

};