import { Component, Input, OnInit, OnDestroy } from '@angular/core';
import { FormControl } from '@angular/forms';
import { FormControlContainer } from '../form.component';
import { HelpKeywordBehavior, TitleAlignment, TextAlignment, HorizontalAlignment, VerticalAlignment, Source } from '../models';
import {
    ITextBoxBase, IAlignedElement, ITitledElement,
    IColumnElement, IDataElement, IElement, INode, IVisual, ISourceReference, IReadOnly, IVerticalAlignedElement
} from '../interfaces';

@Component({
    selector: 'd4-textbox',
    template: require('./textbox.component.html'),
    styles: [require('./textbox.component.css')],
    providers: []
})
export class TextboxComponent
    implements OnInit, OnDestroy, ITextBoxBase, IAlignedElement, ITitledElement,
    IColumnElement, IDataElement, IElement, INode, IVisual, ISourceReference,
    IReadOnly, IVerticalAlignedElement {

    // public instance properties

    @Input('acceptsreturn') AcceptsReturn: boolean = true;
    @Input('acceptstabs') AcceptsTabs: boolean = false;
    @Input('height') Height: number = 1;
    @Input('ispassword') IsPassword: boolean = false;
    @Input('maxlength') MaxLength: number = -1;
    @Input('wordwrap') WordWrap: boolean = false;

    // shared properties
    @Input('textalignment') TextAlignment: TextAlignment = TextAlignment.Left;
    @Input('width') Width: number = 10;
    @Input('maxwidth') MaxWidth: number = -1;
    @Input('columnname') ColumnName: string = '';
    @Input('title') Title: string = '';
    @Input('hint') Hint: string = '';
    @Input('marginleft') MarginLeft: number = 2;
    @Input('marginright') MarginRight: number = 2;
    @Input('margintop') MarginTop: number = 2;
    @Input('marginbottom') MarginBottom: number = 2;
    @Input('helpkeyword') HelpKeyword: string = '';
    @Input('helpkeywordbehavior') HelpKeywordBehavior: HelpKeywordBehavior = HelpKeywordBehavior.KeywordIndex;
    @Input('helpstring') HelpString: string = '';
    // NOTE: An empty value here means that this component will not be added to 
    // a dictionary of named (id-given) components
    @Input('name') Name: string = ''; 
    @Input('userdata') UserData: Object = {}; // TODO: Figure out how to represent the C# Object here (because the JS object is obviously different)
    @Input('readonly') ReadOnly: boolean = false;
    //_source: ISource;
    @Input('source') Source: ISourceReference = null;
    //set Source(val: ISource) { // NOTE: In DFD's this is flattened to a simple string, though it is documented as an ISource
    //    this._source = new Source(val.Name); // TODO: Figure this out (it won't work)
    //}
    //get Source() {
    //    return this._source;
    //}
    @Input('nilifblank') NilIfBlank: boolean = false;
    @Input('titlealignment') TitleAlignment: TitleAlignment = TitleAlignment.Top;
    @Input('verticalalignment') VerticalAlignment: VerticalAlignment = VerticalAlignment.Top;
    @Input('visible') Visible: boolean = true;

    private controlId: string;
    private formControl: FormControl;

    constructor(private _form: FormControlContainer) {}

    ngOnInit() {
        let sourceAndControl = this.ColumnName.split('.');
        this.controlId = sourceAndControl[1];

        this.formControl = new FormControl(this.controlId, null);
        if (this.Source) {
            this.Source = this._form.getSource();
            this._form.addControl(this.controlId, this.formControl, this.Source.Name);
            //get source
            
        }
    }

    ngOnDestroy() {
        if (this.Source) {
            this._form.removeControl(this.controlId);
        }
    }

}