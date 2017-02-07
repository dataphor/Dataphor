import { Component, Input, OnInit, OnDestroy } from '@angular/core';
import { FormControl } from '@angular/forms';
import { FormControlContainer } from '../form.component';
import { Textbox } from '../elements';

@Component({
    selector: 'd4-textbox',
    template: require('./textbox.component.html'),
    styles: [require('./textbox.component.css')],
    providers: []
})
export class TextboxComponent extends Textbox implements OnInit, OnDestroy{

  
    //// NOTE: An empty value here means that this component will not be added to 
    //// a dictionary of named (id-given) components
    //@Input('readonly') ReadOnly: boolean = false;
    ////_source: ISource;
    //@Input('source') Source: ISourceReference = null;
    ////set Source(val: ISource) { // NOTE: In DFD's this is flattened to a simple string, though it is documented as an ISource
    ////    this._source = new Source(val.Name); // TODO: Figure this out (it won't work)
    ////}
    ////get Source() {
    ////    return this._source;
    ////}
    //@Input('nilifblank') NilIfBlank: boolean = false;
    //@Input('titlealignment') TitleAlignment: TitleAlignment = TitleAlignment.Top;
    //@Input('verticalalignment') VerticalAlignment: VerticalAlignment = VerticalAlignment.Top;
    //@Input('visible') Visible: boolean = true;

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