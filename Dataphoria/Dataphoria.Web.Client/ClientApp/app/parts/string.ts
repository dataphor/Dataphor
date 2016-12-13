import { Component, Input } from '@angular/core';
import { Observable } from "rxjs/Rx";

@Component({
    selector: 'string-editor',
    template: `
    <dl>
      <dt>{{propertyName}}</dt>
      <dd>
        <input
          type="text"  
          [(ngModel)]="entity[propertyName]"  />
      </dd>
    </dl>`,
})
export class StringEditor {

    @Input() public propertyName: string;
    @Input() public entity: any;
};