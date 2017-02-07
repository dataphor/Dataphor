import { Component, Input } from '@angular/core';

@Component({
    selector: 'd4-sourceaction',
    template: ``
})
export class SourceActionComponent {
    
    @Input() name: string;
    @Input() source: string;
    @Input() text: string;
    // TODO: Figure out what to do with actions
    @Input() action: string;

}
