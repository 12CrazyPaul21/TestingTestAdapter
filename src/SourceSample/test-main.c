#include <stdio.h>
#include <string.h>
#include <assert.h>

#include <Windows.h>
#include <crtdbg.h>

#pragma optimize("", off)
__declspec(dllexport)
extern int testing_adapter_indicator_v1(void)
{
	return 1;
}
#pragma optimize("", on)

static int t_argc = 0;
static char** t_argv = NULL;

static char* t_getenv(const char* name)
{
	DWORD n, r;
	char* buf;

retry:
	n = GetEnvironmentVariableA(name, NULL, 0);
	if (n == 0) {
		return NULL;
	}

	buf = malloc(n);
	if (buf == NULL) {
		abort();
	}

	r = GetEnvironmentVariableA(name, buf, n);
	if (r == 0 || r >= n) {
		free(buf);
		goto retry;
	}

	return buf;
}

#define TEST(name)                                                             \
  static int test_##name##_line = __LINE__;                                    \
  static int test_##name(void)

TEST(one)
{
	int i;
	for (i = 0; i < 20; i++)
	{
		Sleep(1000);
	}
	return 0;
}

TEST(two)
{
	char* path = t_getenv("PATH");
	char* myvar1 = t_getenv("MyVar1");
	char* myvar2 = t_getenv("MyVar2");
	char* myvar3 = t_getenv("MyVar3");
	char* myvar4 = t_getenv("MyVar4");

	assert(t_argc == 3);
	assert(strcmp(t_argv[0], "arg1") == 0);
	assert(strcmp(t_argv[1], "arg2") == 0);
	assert(strcmp(t_argv[2], "arg3") == 0);

	assert(path != NULL);
	assert(myvar1 != NULL);
	assert(myvar2 != NULL);

	assert(strstr(path, "c:\\bingoDir;c:\\fooDir;c:\\barDir") != NULL);
	assert(strstr(path, "c:\\myPathDir") != NULL);
	assert(strcmp(myvar1, "MyValue1") == 0);
	assert(strcmp(myvar2, "MyValue2.0") == 0);
	assert(strcmp(myvar3, "MyValue3") == 0);
	assert(strcmp(myvar4, "MyValue4") == 0);

	free(path);
	free(myvar1);
	free(myvar2);
	free(myvar3);
	free(myvar4);
	return 0;
}

TEST(three)
{
	fprintf(stderr, "expected failure\n");
	return 1;
}

TEST(four)
{
	return 2;
}

TEST(five)
{
	return 3;
}

static void list_tests(void)
{
	printf("one\n");
	printf("two\n");
	printf("three\n");
	printf("four\n");
	printf("five\n");
}

static void print_all_details(void)
{
	const unsigned char *p = (const unsigned char*)__FILE__;
	char *file = malloc(strlen(__FILE__) * 2 + 1);
	char *q = file;

	while (*p) {
		if (*p == '\\') {
			*q++ = '\\';
            *q++ = '\\';
		} else {
			*q++ = *p;
		}
        p++;
    }
    *q = '\0';

	printf("{\n");
	printf("  \"one\": [\n");
	printf("    {\"file\": \"%s\"},\n", file);
	printf("    {\"line\": \"%d\"},\n", test_one_line);
	printf("    {\"suite\": \"mod1\"},\n");
	printf("    {\"category\": \"c1\"},\n");
	printf("    {\"arg\": \"one\"},\n");
	printf("    {\"var\": \"foo: bar : end\"},\n");
	printf("    {\"empty\": \"\"}\n");
	printf("  ],\n");
	printf("  \"two\": [\n");
	printf("    {\"file\": \"%s\"},\n", file);
	printf("    {\"line\": \"%d\"},\n", test_two_line);
	printf("    {\"suite\": \"mod1\"},\n");
	printf("    {\"category\": \"c1\"},\n");
	printf("    {\"arg\": \"two -- arg1 arg2 arg3\"},\n");
	printf("    {\"env\": \"MyVar2=MyValue2.0\"},\n");
	printf("    {\"env\": \"MyVar3=MyValue3\"},\n");
	printf("    {\"env\": \"MyVar4=MyValue4\"}\n");
	printf("  ],\n");
	printf("  \"three\": [\n");
	printf("    {\"file\": \"%s\"},\n", file);
	printf("    {\"line\": \"%d\"},\n", test_three_line);
	printf("    {\"suite\": \"expected\"},\n");
	printf("    {\"arg\": \"three\"},\n");
	printf("    {\"xfail\": \"true\"}\n");
	printf("  ],\n");
	printf("  \"four\": [\n");
	printf("    {\"file\": \"%s\"},\n", file);
	printf("    {\"line\": \"%d\"},\n", test_four_line);
	printf("    {\"suite\": \"expected\"},\n");
	printf("    {\"arg\": \"four\"}\n");
	printf("  ],\n");
	printf("  \"five\": [\n");
	printf("    {\"file\": \"%s\"},\n", file);
	printf("    {\"line\": \"%d\"},\n", test_five_line);
	printf("    {\"suite\": \"expected\"},\n");
	printf("    {\"arg\": \"five\"}\n");
	printf("  ]\n");
	printf("}\n");

	free(file);
}

static int run_test(const char* test_name, int i, int (*func)(void))
{
	int result = func();
	if (result == 0) {
		printf("ok - %s\n", test_name);
		return 0;
	}
	else if (result == 2) {
		printf("ok - %s # SKIP\n", test_name);
		return 0;
	}
	else if (result == 3) {
		printf("not ok - %s # TODO\n", test_name);
		return 0;
	}
	else {
		printf("not ok - %s\n", test_name);
		return 1;
	}
}

static int run_one_test(const char* test_name, int (*func)(void))
{
	printf("TAP version 13\n");
	printf("1..1\n");
	return run_test(test_name, 1, func);
}

static int run_all_test(void)
{
	printf("TAP version 13\n");
	printf("1..5\n");
	return run_test("one", 1, test_one) |
		run_test("two", 2, test_two) |
		run_test("three", 3, test_three) |
		run_test("four", 4, test_four) |
		run_test("five", 5, test_five);
}

int main(int argc, char* argv[])
{
	SetErrorMode(SEM_FAILCRITICALERRORS | SEM_NOGPFAULTERRORBOX |
		SEM_NOOPENFILEERRORBOX);
	_CrtSetReportMode(_CRT_ASSERT, _CRTDBG_MODE_FILE);
	_CrtSetReportFile(_CRT_ASSERT, _CRTDBG_FILE_STDERR);
	_CrtSetReportMode(_CRT_ERROR, _CRTDBG_MODE_FILE);
	_CrtSetReportFile(_CRT_ERROR, _CRTDBG_FILE_STDERR);

	if (argc > 1) {
		int i;
		for (i = 1; i < argc; i++) {
			if (strcmp(argv[i], "--") == 0) {
				t_argc = argc - i - 1;
				t_argv = &argv[i + 1];
				argc = i;
				break;
			}
		}
	}

	if (argc == 1) {
		return run_all_test();
	}
	else if (argc == 2) {
		if (strcmp(argv[1], "--list") == 0) {
			list_tests();
			return 0;
		}
		else if (strcmp(argv[1], "--list-details") == 0) {
			print_all_details();
			return 0;
		}
		else if (strcmp(argv[1], "one") == 0) {
			return run_one_test("one", test_one);
		}
		else if (strcmp(argv[1], "two") == 0) {
			return run_one_test("two", test_two);
		}
		else if (strcmp(argv[1], "three") == 0) {
			return run_one_test("three", test_three);
		}
		else if (strcmp(argv[1], "four") == 0) {
			return run_one_test("four", test_four);
		}
		else if (strcmp(argv[1], "five") == 0) {
			return run_one_test("five", test_five);
		}
	}
	return 0;
}
