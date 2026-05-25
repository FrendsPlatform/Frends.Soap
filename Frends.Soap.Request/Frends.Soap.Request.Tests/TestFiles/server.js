// SOAP test server using ONLY built-in Node.js modules (no npm install required)
const http = require('http');
const https = require('https');
const fs = require('fs');
const url = require('url');
function buildSoapEnvelope(body, version) {
    const ns = version === '1.2'
        ? 'http://www.w3.org/2003/05/soap-envelope'
        : 'http://schemas.xmlsoap.org/soap/envelope/';
    return `<?xml version="1.0" encoding="UTF-8"?>\n<soap:Envelope xmlns:soap="${ns}">\n    <soap:Body>\n        ${body}\n    </soap:Body>\n</soap:Envelope>`;
}
function buildSoapFault(message, version) {
    const ns = version === '1.2'
        ? 'http://www.w3.org/2003/05/soap-envelope'
        : 'http://schemas.xmlsoap.org/soap/envelope/';
    const faultBody = version === '1.2'
        ? `<soap:Fault><soap:Code><soap:Value>soap:Receiver</soap:Value></soap:Code><soap:Reason><soap:Text xml:lang="en">${message}</soap:Text></soap:Reason></soap:Fault>`
        : `<soap:Fault><faultcode>soap:Server</faultcode><faultstring>${message}</faultstring></soap:Fault>`;
    return `<?xml version="1.0" encoding="UTF-8"?>\n<soap:Envelope xmlns:soap="${ns}">\n    <soap:Body>\n        ${faultBody}\n    </soap:Body>\n</soap:Envelope>`;
}
function getSampleWsdl() {
    return `<?xml version="1.0" encoding="UTF-8"?>
<definitions xmlns="https://schemas.xmlsoap.org/wsdl/"
             xmlns:tns="https://example.com/weatherservice"
             xmlns:xsd="http://www.w3.org/2001/XMLSchema"
             xmlns:soap="http://schemas.xmlsoap.org/wsdl/soap/"
             targetNamespace="https://example.com/weatherservice"
             name="WeatherService">
    <types>
        <xsd:schema targetNamespace="https://example.com/weatherservice">
            <xsd:element name="GetWeather">
                <xsd:complexType>
                    <xsd:sequence>
                        <xsd:element name="City" type="xsd:string"/>
                    </xsd:sequence>
                </xsd:complexType>
            </xsd:element>
        </xsd:schema>
    </types>
</definitions>`;
}
function buildHeaderEchoEnvelope(headers, version) {
    const customOne = headers['x-custom-header-one'] || headers['x-test-header-one'] || '';
    const customTwo = headers['x-custom-header-two'] || headers['x-test-header-two'] || '';
    return buildSoapEnvelope(`<HeaderResponse xmlns="https://example.com/service"><HeaderOne>${customOne}</HeaderOne><HeaderTwo>${customTwo}</HeaderTwo></HeaderResponse>`, version);
}
function handleRequest(req, res) {
    let body = '';
    req.on('data', chunk => { body += chunk; });
    req.on('end', () => {
        const { pathname } = url.parse(req.url);
        const method = req.method;
        if (method === 'GET' && pathname === '/health') {
            res.writeHead(200, { 'Content-Type': 'text/plain' });
            res.end('OK');
            return;
        }

        if (method === 'GET' && pathname === '/wsdl') {
            res.writeHead(200, { 'Content-Type': 'application/wsdl+xml' });
            res.end(getSampleWsdl());
            return;
        }

        if (method !== 'POST') {
            res.writeHead(405);
            res.end('Method Not Allowed');
            return;
        }
        const version = (req.headers['content-type'] || '').includes('soap+xml') ? '1.2' : '1.1';
        const authHeader = req.headers['authorization'] || '';
        const token = authHeader.startsWith('Bearer ') ? authHeader.slice(7) : null;
        const basicToken = Buffer.from('basic-user:basic-pass').toString('base64');
        switch (pathname) {
            case '/soap/echo':
                res.writeHead(200, { 'Content-Type': 'application/xml' });
                res.end(buildSoapEnvelope('<EchoResponse xmlns="https://example.com/service"><Result>Echo received</Result></EchoResponse>', version));
                break;
            case '/soap/custom-headers':
                res.writeHead(200, { 'Content-Type': 'application/xml' });
                res.end(buildHeaderEchoEnvelope(req.headers, version));
                break;
            case '/soap/redirect-start':
                res.writeHead(307, { Location: '/soap/echo' });
                res.end();
                break;
            case '/soap/slow':
                setTimeout(() => {
                    res.writeHead(200, { 'Content-Type': 'application/xml' });
                    res.end(buildSoapEnvelope('<SlowResponse xmlns="https://example.com/service"><Status>Slow response</Status></SlowResponse>', version));
                }, 4000);
                break;
            case '/soap/basic-auth':
                if (authHeader !== `Basic ${basicToken}`) {
                    res.writeHead(401, { 'Content-Type': 'application/xml' });
                    res.end(buildSoapFault('Unauthorized', version));
                    return;
                }

                res.writeHead(200, { 'Content-Type': 'application/xml' });
                res.end(buildSoapEnvelope('<BasicAuthResponse xmlns="https://example.com/service"><Status>Basic auth accepted</Status></BasicAuthResponse>', version));
                break;
            case '/soap/client-cert-auth': {
                const peerCert = req.socket.getPeerCertificate();
                const hasClientCert = peerCert && Object.keys(peerCert).length > 0;
                const subjectCn = hasClientCert && peerCert.subject ? peerCert.subject.CN : '';

                if (!hasClientCert) {
                    res.writeHead(401, { 'Content-Type': 'application/xml' });
                    res.end(buildSoapFault('Client certificate required', version));
                    return;
                }

                if (subjectCn !== 'Test Client Certificate') {
                    res.writeHead(403, { 'Content-Type': 'application/xml' });
                    res.end(buildSoapFault('Client certificate rejected', version));
                    return;
                }

                res.writeHead(200, { 'Content-Type': 'application/xml' });
                res.end(buildSoapEnvelope('<EchoResponse xmlns="https://example.com/service"><Result>Client certificate accepted</Result></EchoResponse>', version));
                break;
            }
            case '/soap11/success':
                res.writeHead(200, { 'Content-Type': 'text/xml' });
                res.end(buildSoapEnvelope('<SuccessResponse xmlns="https://example.com/service"><Status>Success</Status></SuccessResponse>', '1.1'));
                break;
            case '/soap/success':
                res.writeHead(200, { 'Content-Type': 'application/xml' });
                res.end(buildSoapEnvelope('<SuccessResponse xmlns="https://example.com/service"><Status>Success</Status></SuccessResponse>', version));
                break;
            case '/soap/protected':
                if (!token || token !== 'valid-test-token') {
                    res.writeHead(401, { 'Content-Type': 'application/xml' });
                    res.end(buildSoapFault('Unauthorized', version));
                    return;
                }
                res.writeHead(200, { 'Content-Type': 'application/xml' });
                res.end(buildSoapEnvelope('<ProtectedResponse xmlns="https://example.com/service"><Data>Secret</Data></ProtectedResponse>', version));
                break;
            case '/soap/fault':
                res.writeHead(500, { 'Content-Type': 'application/xml' });
                res.end(buildSoapFault('Test SOAP Fault', '1.1'));
                break;
            case '/soap/fault12':
                res.writeHead(500, { 'Content-Type': 'application/soap+xml' });
                res.end(buildSoapFault('Test SOAP Fault for SOAP 1.2', '1.2'));
                break;
            case '/soap/error':
                res.writeHead(500, { 'Content-Type': 'text/plain' });
                res.end('Internal Server Error');
                break;
            case '/soap/notfound':
                res.writeHead(404, { 'Content-Type': 'text/plain' });
                res.end('Not Found');
                break;
            case '/soap/trace': {
                const traceparent = req.headers['traceparent'] || 'not-provided';
                const tracestate = req.headers['tracestate'] || 'not-provided';
                res.writeHead(200, { 'Content-Type': 'application/xml' });
                res.end(buildSoapEnvelope(`<TraceResponse xmlns="https://example.com/service"><ReceivedTraceparent>${traceparent}</ReceivedTraceparent><ReceivedTracestate>${tracestate}</ReceivedTracestate></TraceResponse>`, version));
                break;
            }
            default:
                res.writeHead(404);
                res.end('Not Found');
        }
    });
}
http.createServer(handleRequest).listen(8080, () => console.log('HTTP on 8080'));
const tlsOpts = {
    key: fs.readFileSync('/app/server-key.pem'),
    cert: fs.readFileSync('/app/server-cert.pem'),
    requestCert: true,
    rejectUnauthorized: false
};
https.createServer(tlsOpts, handleRequest).listen(8443, () => console.log('HTTPS on 8443'));
