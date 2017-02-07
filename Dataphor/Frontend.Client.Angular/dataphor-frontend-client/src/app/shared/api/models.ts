export declare interface IResponse {
    value: any;
}

export class ResponseSingle implements IResponse {
    value: Object;
    constructor(_value: Object) {
        this.value = _value;
    }
}

export class ResponseSet implements IResponse {
    value: Object[];
    constructor(_value: Object[]) {
        this.value = _value;
    }
}

//export type Response = ResponseSingle | ResponseSet;