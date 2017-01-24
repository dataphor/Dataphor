import { Input, OnInit, OnDestroy, ContentChildren, QueryList } from '@angular/core';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';
import { SourceLinkType, SourceLink, SurrogateSourceLink, DetailSourceLink } from '../source';
import { IFrame, IFrameInterface, INotebookFramePage, IAction } from '../interfaces';
import { InterfaceService } from '../interface';
import { NodeService } from '../nodes/index';
import { BaseNotebookPage, Frame, FrameInterface } from './index';

const DefaultWidth: number = 10;
const DefaultAutoUpdateInterval: number = 200;

export class NotebookFramePage extends BaseNotebookPage implements OnInit, IFrame, INotebookFramePage {

    constructor(sourceLinkType: SourceLinkType) {
        super();
        this.SourceLinkType = sourceLinkType;

    }

    ngOnInit() {

        

        // TODO: Fetch Action Observable for BeforeCloseEmbedded
        // TODO: Fetch SourceLink Observable for SourceLink
        if (this.MenuText === '') {
            if (this.Title !== '') {
                this.MenuText = this.Title;
            } else {
                // this.MenuText = (new System.Resources.ResourceManager("Alphora.Dataphor.Frontend.Client.Windows.Strings", typeof(Frame).Assembly).GetString("CFrameDefaultMenuText"));
                // aka some default text
            }
        }
    }


    
    
    @Input('sourcelink') __sourceLink: string = '';
    private _sourceLink: SourceLink;
    get SourceLink(): SourceLink {
        return this._sourceLink;
    }
    @Input('postbeforeclosingembedded') PostBeforeClosingEmbedded: boolean = true;
    @Input('beforecloseembedded') __beforeCloseEmbedded: string = '';
    private _beforeCloseEmbedded: IAction;
    get BeforeCloseEmbedded(): IAction {
        return this._beforeCloseEmbedded;
    }
    set BeforeCloseEmbedded(value: IAction) {
        this._beforeCloseEmbedded = value;
    }
    @Input('document') Document: string = '';
    @Input('filter') Filter: string = '';
    // Don't know that we need frameInterface
    private _frameInterfaceNode: FrameInterface;
    get FrameInterfaceNode(): IFrameInterface {
        return this._frameInterfaceNode;
    }

    
    @Input('menutext') MenuText: string = '';
    @Input('loadasselected') LoadAsSelected: boolean = true;

    private _sourceLinkType: SourceLinkType;
    get SourceLinkType(): SourceLinkType {
        return this._sourceLinkType;
    }
    set SourceLinkType(value: SourceLinkType) {
        if (this._sourceLinkType !== value) {
            this._sourceLinkType = value;
            if (this._sourceLinkType === SourceLinkType.None) {
                this._sourceLink = null;
            } else {
                if (this._sourceLinkType === SourceLinkType.Surrogate) {
                    this._sourceLink = new SurrogateSourceLink(this);
                } else if (this._sourceLinkType === SourceLinkType.Detail) {
                    this._sourceLink = new DetailSourceLink(this);
                }
                if (this._frameInterfaceNode !== null) {
                    this._sourceLink.TargetSource = this._frameInterfaceNode.MainSource;
                }
            }

        }
    }


    private IsSelected(): boolean {
        // TODO: Figure out a way of getting that information down into this context
        return true;
    }

    private ShouldLoad(): boolean {
        return ((this.Document !== '') && (this.IsSelected() || this.LoadAsSelected));
    }


    
};