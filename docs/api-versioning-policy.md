# TMS API Versioning & Sunset Policy

## 1. Breaking Changes (Requires New Major Version)
* Removing or renaming JSON fields or query parameters.
* Altering HTTP status code outputs for existing paths.
* Adding strict, new validation rules onto existing endpoints.

## 2. Additive Changes (Non-Breaking)
* Introducing entirely new resource endpoints.
* Appending optional fields to an existing JSON response body.
* Adding optional query parameter filters.

## 3. Sunset Commitment
* Legacy versions (V1) are guaranteed a minimum lifecycle window of 6 months following the official deployment of their successor version (V2).