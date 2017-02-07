import { Injectable } from '@angular/core';
import { Headers, Http, Response } from '@angular/http';
import 'rxjs/add/operator/toPromise';
import { config } from '../index';
import { IResponse, ResponseSingle, ResponseSet } from './models';

@Injectable()
export class APIService {

    constructor(private http: Http) { }


    get(query: string) {
        let headers = new Headers({
            'Content-Type': 'application/json',
            // TODO: Comment back in once we've added localhost to CORS on the service end'
            // Reference: http://stackoverflow.com/questions/18234366/restful-webservice-how-to-set-headers-in-java-to-accept-xmlhttprequest-allowed
            //'FHIR-Service' : fhirServiceUri
        });

        let uri = config.api.serviceBase + '/query/' + encodeURI(query);

        return this.http
            .get(uri, { headers: headers })
            .toPromise()
            .then(res => res.json())
            .catch(this.handleError);
    }

    post(query: Object) {
        let headers = new Headers({
            'Content-Type': 'application/json'
        });

        let uri = config.api.serviceBase + '/query'
            //+ encodeURI(query);

        return this.http
            .post(uri, query, { headers: headers })
            .toPromise()
            .then(res => res.json())
            .catch(this.handleError);
    }

    handleResponse(response: any): any {
        let handledResponse = '';
        let responseModel: IResponse;
        if (Array.isArray(response)) {
            responseModel = new ResponseSet(response);
            for (let r of response) {
                handledResponse += JSON.stringify(r) + '\r\n';
            }
        }
        else {
            responseModel = new ResponseSingle(response);
            handledResponse += JSON.stringify(responseModel) + '\r\n';
        }
        return handledResponse;
    };

    private handleError(error: any) {
        error = error.json();
        var result = error.status + ': ' + error.statusText;
        result += '\r\n';
        result += error.responseText;
        console.error(error);
        return Promise.reject(error || error);
    }
}