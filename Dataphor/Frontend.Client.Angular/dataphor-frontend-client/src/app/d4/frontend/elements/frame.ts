import { Input, OnInit, OnDestroy, ViewChildren } from '@angular/core';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';

import { IFrame, IFrameInterface } from '../interfaces';
import { InterfaceService } from '../interface';
import { NodeService } from '../nodes/index';

import { Element } from './index';

export class Frame extends Element implements OnInit, IFrame {

    @Input('document') Document: string = '';
    // TODO: Get/set this node
    @Input('frameinterfacenode') FrameInterfaceNode: IFrameInterface;
    @Input('menutext') MenuText: string = '';

    constructor() {
        super();
        this.MarginLeft = 0;
        this.MarginRight = 0;
        this.MarginTop = 0;
        this.MarginBottom = 0;
    }

    private _sourceLinkType: SourceLinkType;
    get SourceLinkType(): SourceLinkType {
        return this._sourceLinkType;
    }
    set SourceLinkType(value: SourceLinkType) {
        // TODO: Port logic from Frame.cs
        this._sourceLinkType = value;
    }

    private _sourceLink: SourceLink;
    get SourceLink(): SourceLink {
        return this._sourceLink;
    }
    set SourceLink(value: SourceLink) {
        this._sourceLink = value;
    }


    // TODO: Add additional frame coolness


};