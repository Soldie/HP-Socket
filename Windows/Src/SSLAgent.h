/*
 * Copyright: JessMA Open Source (ldcsaa@gmail.com)
 *
 * Author	: Bruce Liang
 * Website	: http://www.jessma.org
 * Project	: https://github.com/ldcsaa
 * Blog		: http://www.cnblogs.com/ldcsaa
 * Wiki		: http://www.oschina.net/p/hp-socket
 * QQ Group	: 75375912, 44636872
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
 
#pragma once

#include "TcpAgent.h"
#include "SSLHelper.h"

#ifdef _SSL_SUPPORT

class CSSLAgent : public CTcpAgent
{
public:
	virtual BOOL IsSecure() {return TRUE;}
	virtual BOOL SendPackets(CONNID dwConnID, const WSABUF pBuffers[], int iCount);

	virtual BOOL SetupSSLContext(int iVerifyMode = SSL_VM_NONE, LPCTSTR lpszPemCertFile = nullptr, LPCTSTR lpszPemKeyFile = nullptr, LPCTSTR lpszKeyPassword = nullptr, LPCTSTR lpszCAPemCertFileOrPath = nullptr)
		{return m_sslCtx.Initialize(SSL_SM_CLIENT, iVerifyMode, FALSE, (LPVOID)lpszPemCertFile, (LPVOID)lpszPemKeyFile, (LPVOID)lpszKeyPassword, (LPVOID)lpszCAPemCertFileOrPath, nullptr);}

	virtual BOOL SetupSSLContextByMemory(int iVerifyMode = SSL_VM_NONE, LPCSTR lpszPemCert = nullptr, LPCSTR lpszPemKey = nullptr, LPCSTR lpszKeyPassword = nullptr, LPCSTR lpszCAPemCert = nullptr)
		{return m_sslCtx.Initialize(SSL_SM_CLIENT, iVerifyMode, TRUE, (LPVOID)lpszPemCert, (LPVOID)lpszPemKey, (LPVOID)lpszKeyPassword, (LPVOID)lpszCAPemCert, nullptr);}

	virtual void CleanupSSLContext()
		{m_sslCtx.Cleanup();}

	virtual BOOL StartSSLHandShake(CONNID dwConnID);

public:
	virtual void SetSSLAutoHandShake(BOOL bAutoHandShake)	{m_bSSLAutoHandShake = bAutoHandShake;}
	virtual BOOL IsSSLAutoHandShake	()						{return m_bSSLAutoHandShake;}

	virtual BOOL GetSSLSessionInfo(CONNID dwConnID, EnSSLSessionInfo enInfo, LPVOID* lppInfo);

protected:
	virtual EnHandleResult FireConnect(TSocketObj* pSocketObj);
	virtual EnHandleResult FireReceive(TSocketObj* pSocketObj, const BYTE* pData, int iLength);
	virtual EnHandleResult FireClose(TSocketObj* pSocketObj, EnSocketOperation enOperation, int iErrorCode);

	virtual BOOL CheckParams();
	virtual void PrepareStart();
	virtual void Reset();

	virtual void OnWorkerThreadEnd(THR_ID dwThreadID);

protected:
	virtual BOOL StartSSLHandShake(TSocketObj* pSocketObj);

private:
	void DoSSLHandShake(TSocketObj* pSocketObj);

private:
	friend EnHandleResult ProcessHandShake<>(CSSLAgent* pThis, TSocketObj* pSocketObj, CSSLSession* pSession);
	friend EnHandleResult ProcessReceive<>(CSSLAgent* pThis, TSocketObj* pSocketObj, CSSLSession* pSession, const BYTE* pData, int iLength);
	friend BOOL ProcessSend<>(CSSLAgent* pThis, TSocketObj* pSocketObj, CSSLSession* pSession, const WSABUF * pBuffers, int iCount);

public:
	CSSLAgent(ITcpAgentListener* pListener)
	: CTcpAgent(pListener)
	, m_sslPool(m_sslCtx)
	, m_bSSLAutoHandShake(TRUE)
	{

	}

	virtual ~CSSLAgent()
	{
		ENSURE_STOP();
	}

private:
	BOOL m_bSSLAutoHandShake;

	CSSLContext m_sslCtx;
	CSSLSessionPool m_sslPool;
};

#endif