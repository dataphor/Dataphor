import { Input, OnInit, OnDestroy, ViewChildren } from '@angular/core';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';

import { IElement, HelpKeywordBehavior, IVisual } from '../interfaces';
import { InterfaceService } from '../interface';
import { NodeService } from '../nodes/index';
import { Node } from '../nodes/index';

const AverageChars: string = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
const DefaultMarginLeft: number = 2;
const DefaultMarginRight: number = 2;
const DefaultMarginTop: number = 2;
const DefaultMarginBottom: number = 2;

export class Element extends Node implements OnInit, IElement, IVisual {

    constructor() {
        super();
    }

    @Input('helpkeyword') HelpKeyword: string = '';
    @Input('helpkeywordbehavior') HelpKeywordBehavior: HelpKeywordBehavior = HelpKeywordBehavior.KeywordIndex;
    @Input('helpstring') HelpString: string = '';
    @Input('hint') Hint: string = '';
    @Input('marginbottom') MarginBottom: number = DefaultMarginBottom;
    @Input('marginleft') MarginLeft: number = DefaultMarginLeft;
    @Input('marginright') MarginRight: number = DefaultMarginRight;
    @Input('margintop') MarginTop: number = DefaultMarginTop;
    @Input('tabstop') TabStop: boolean = true;
    @Input('style') Style: string = '';

    GetHint(): string {
        return this.Hint;
    }
    GetTabStop(): boolean {
        return this.TabStop;
    }

    @Input('visible') Visible: boolean = true;

    // TODO: Implement visibility rules below
    /// <para>If the component is a control, when Visible is set to false 
	/// the control will not be visible on the form. If the control is the
	/// parent of other controls, all decendent controls will also be hidden.</para>
	/// <para>If the component is an action, when Visible is set to false
	/// the control(s) associated with the action will not be 
	/// visible on the form.</para>
    GetVisible(): boolean {
        return this.Visible;
    }



};