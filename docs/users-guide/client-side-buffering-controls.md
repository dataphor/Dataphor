# Client-side Buffering Controls

## Introduction

The CLI performs client-side buffering of result sets to minimize network round trips. There are two properties that control various aspects of this buffering mechanism, FetchAtOpen and FetchCount. Once the session is connected, changing these settings has no affect.

## FetchAtOpen

The FetchAtOpen property determines whether the results of the open call will fetch the initial buffer. This property is used by the Frontend Clients to minimize network round trips.

The default value for this property is false.

## FetchCount

The FetchCount property determines the size in rows of the client-side buffer.

The default value for this setting is 20. Setting the value of this property to 1 effectively disables client-side buffering.
