import {
    Component, OnInit, ViewChildren, QueryList, OnDestroy, forwardRef, Injector
} from '@angular/core';
import {
    AbstractControl, FormControl, FormGroup, ControlContainer, Validators, FormGroupDirective
} from '@angular/forms';

export abstract class FormControlContainer {
    abstract getControl(name: string): AbstractControl | null;
    abstract addSource(name: string, control: FormGroup): AbstractControl | null;
    abstract updateGroup(name: string, control: AbstractControl, model: Object): void;
    abstract addControl(name: string, control: FormControl, source: string): void;
    abstract removeControl(name: string): void;
    abstract setMainSource(name: string): void;
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

    getControl(name: string): AbstractControl | null {
        if (!this.form.contains(name)) return null;
        else {
            return this.form.get(name);
        }
    }

    addSource(name: string, control: FormGroup): AbstractControl | null {
        if (!this.form.contains(name)) {
            this.form.addControl(name, control);
        }
        return this.form.get(name);
    }

    // {"Main.ID":"Admin","Main.Name":"Administrator"}
    // {"Main.ID":"System", "Main.Name":"System User" }
    // Updates group with new values given in object
    updateGroup(name: string, control: AbstractControl, model: Object): void {
        control.patchValue(model);
    }

    addControl(name: string, control: FormControl, source: string): void {

        //If the source has not been created yet, go ahead and create it
        if (!this.form.contains(source)) {
            this.form.addControl(source, new FormGroup({ control }));
        } else {
            let group = this.getControl(source) as FormGroup;
            group.addControl(name, control)
        }
        
    }

    removeControl(name: string): void {
        this.form.removeControl(name);
    }

    setMainSource(name: string): void {
        // TODO: Set once we know how this translates to the web
    }
}