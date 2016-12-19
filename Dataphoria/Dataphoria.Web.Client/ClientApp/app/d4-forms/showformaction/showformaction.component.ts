import { Component, Input } from '@angular/core';
import { FormControl } from '@angular/forms';
import { FormControlContainer } from '../form.component';

@Component({
    selector: 'd4-showformaction',
    template: ``
})
export class ShowFormActionComponent {
    
    @Input() name: string = '';
    @Input() text: string = '';
    @Input() document: string = '';

    sourcelink = {
        source: ''
    };

    @Input() sourcelink.source = '';
    // TODO: Figure out how to map different sourcelinks to this
    // e.g., sourcelink.source, sourcelink.masterkeynames, etc.
    @Input() sourcelinktype: string = '';
    
    constructor(private _form: FormControlContainer) {

        // TODO: Finish

    }

}
