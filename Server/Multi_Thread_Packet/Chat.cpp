#include "stdafx.h"
#include "Chat.h"


void Chat::AddMsg(const string& msg)
{
	unique_lock<shared_mutex> lock(mutex);

	if (msgs.size() >= 20)
		msgs.pop();

	msgs.push(msg);
}