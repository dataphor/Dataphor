import {
    Component, OnInit, ViewChildren, QueryList, OnDestroy, forwardRef, Injector
} from '@angular/core';
import {
    FormControl,
    FormGroup,
    ControlContainer,
    Validators,
    FormGroupDirective
} from '@angular/forms';


export abstract class FormControlContainer {
    abstract addControl(name: string, control: FormControl): void;
    abstract removeControl(name: string): void;
}

export const formGroupContainerProvider: any = {
    provide: FormControlContainer,
    useExisting: forwardRef(() => FormComponent)
};

@Component({
    selector: 'd4-form',
    template: `
    <form [formGroup]="form" novalidate>
        <ng-content></ng-content>
        <div>
            <button type="button" (click)="onSubmit()">Submit</button>
        </div>
    </form>
  `,
    providers: [formGroupContainerProvider]
})
export class FormComponent implements FormControlContainer {

    form = new FormGroup({});

    onSubmit() {
        console.log(this.form.value);
    }

    addControl(name: string, control: FormControl): void {
        this.form.addControl(name, control);
    }

    removeControl(name: string): void {
        this.form.removeControl(name);
    }
}