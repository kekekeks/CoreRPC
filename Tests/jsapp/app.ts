import {CoreApi} from "./api";
import fetch from "node-fetch";
const apiUrl = process.env['APIADDR']!;
console.error('API URL: '+ apiUrl);
const api = new CoreApi(apiUrl, (url, ri)=>{
    ri.headers={"X-Test": 'test-test'};
    return fetch(url, ri);
});

async function work()
{
    try {
        const api1Res = await api.iMyRpc1.foo(3);
        console.error(api1Res);
        const api2Res = await api.iMyRpc2.bar(3);
        console.error(api2Res);
        const api3Res = await api.myRpc3.foo();
        console.error(api3Res);
        const headerRes = await api.contextAwareRpc.getHeader();
        console.error(headerRes);
        const interceptedRes = await api.myRpc3.intercepted(123, 321);
        console.error(interceptedRes);
        const namedRes = await api.mySuperName.echo(123);
        console.error(namedRes);
        if (api1Res === true && api2Res === '3' && api3Res === 'test' 
            && headerRes == 'test-test' && interceptedRes == 'test-test123321'
            && namedRes == 123)
            console.log("OK");
    }catch (e) {
        console.error(e);
    }
}

work();