// idCrypt originally by emoose, updated by Zwip-Zwap Zapony
// Code licensed under GPL 3.0.

#include <windows.h>

#include <stdbool.h>
#include <stdio.h>
#include <bcrypt.h>
#include <ncrypt.h>

#define NT_SUCCESS(Status) (((NTSTATUS)(Status)) >= 0)
#define strlcpy(dest,src,size) do {strncpy((dest),(src),(size)-1); (dest)[(size)-1]='\0';} while (false) // Note: This doesn't return a value

// static string used during key derivation
const char* keyDeriveStatic = "swapTeam\n";

NTSTATUS hash_data(void* pbBuf1, int cbBuf1, void* pbBuf2, int cbBuf2, void* pbBuf3, int cbBuf3, void* pbSecret, int cbSecret, void* output)
{
	BCRYPT_ALG_HANDLE hashAlg;
	NTSTATUS res = ERROR_SUCCESS;
	DWORD unk = 0;

	if (!NT_SUCCESS(res = BCryptOpenAlgorithmProvider(&hashAlg, BCRYPT_SHA256_ALGORITHM, NULL, pbSecret ? BCRYPT_ALG_HANDLE_HMAC_FLAG : 0)))
		return res;

	DWORD hashObjectSize = 0;
	if (!NT_SUCCESS(res = BCryptGetProperty(hashAlg, BCRYPT_OBJECT_LENGTH, (PBYTE)&hashObjectSize, sizeof(DWORD), &unk, 0)))
		return res;

	PBYTE hashObject = (PBYTE)HeapAlloc(GetProcessHeap(), 0, hashObjectSize);
	if (!hashObject)
		return -1;

	DWORD hashSize = 0;
	if (!NT_SUCCESS(res = BCryptGetProperty(hashAlg, BCRYPT_HASH_LENGTH, (PBYTE)&hashSize, sizeof(DWORD), &unk, 0)))
		return res;

	BCRYPT_HASH_HANDLE hashHandle;
	if (!NT_SUCCESS(res = BCryptCreateHash(hashAlg, &hashHandle, hashObject, hashObjectSize, (PBYTE)pbSecret, cbSecret, 0)))
		return res;

	if(pbBuf1)
		if (!NT_SUCCESS(res = BCryptHashData(hashHandle, (PBYTE)pbBuf1, cbBuf1, 0)))
			return res;

	if (pbBuf2)
		if (!NT_SUCCESS(res = BCryptHashData(hashHandle, (PBYTE)pbBuf2, cbBuf2, 0)))
			return res;

	if (pbBuf3)
		if (!NT_SUCCESS(res = BCryptHashData(hashHandle, (PBYTE)pbBuf3, cbBuf3, 0)))
			return res;

	res = BCryptFinishHash(hashHandle, (PBYTE)output, hashSize, 0);
	BCryptCloseAlgorithmProvider(hashAlg, 0);

	HeapFree(GetProcessHeap(), 0, hashObject);

	return res;
}

NTSTATUS crypt_data(bool decrypt, void* pbInput, int cbInput, void* pbEncKey, int cbEncKey, void* pbIV, int cbIV, void* pbOutput, ULONG* cbOutput)
{
	BCRYPT_ALG_HANDLE hashAlg;
	NTSTATUS res = ERROR_SUCCESS;
	DWORD unk = 0;

	if (!NT_SUCCESS(res = BCryptOpenAlgorithmProvider(&hashAlg, BCRYPT_AES_ALGORITHM, NULL, 0)))
		return res;

	DWORD blockSize = 0;
	if (!NT_SUCCESS(res = BCryptGetProperty(hashAlg, BCRYPT_BLOCK_LENGTH, (PBYTE)&blockSize, sizeof(DWORD), &unk, 0)))
		return res;

	BCRYPT_KEY_HANDLE hKey = NULL;
	DWORD keySize = 0;
	if (!NT_SUCCESS(res = BCryptGetProperty(hashAlg, BCRYPT_OBJECT_LENGTH, (PBYTE)&keySize, sizeof(DWORD), &unk, 0)))
		return res;

	BYTE* key = (BYTE*)malloc(keySize);

	if (!NT_SUCCESS(res = BCryptGenerateSymmetricKey(hashAlg, &hKey, key, keySize, (PBYTE)pbEncKey, cbEncKey, 0)))
		return res;

	if(decrypt)
		res = BCryptDecrypt(hKey, (PBYTE)pbInput, cbInput, 0, (PBYTE)pbIV, cbIV, (PBYTE)pbOutput, *cbOutput, cbOutput, BCRYPT_BLOCK_PADDING);
	else
		res = BCryptEncrypt(hKey, (PBYTE)pbInput, cbInput, 0, (PBYTE)pbIV, cbIV, (PBYTE)pbOutput, *cbOutput, cbOutput, BCRYPT_BLOCK_PADDING);

	BCryptDestroyKey(hKey);
	BCryptCloseAlgorithmProvider(hashAlg, 0);

	free(key);

	return res;
}

void PrintUsage(void)
{
	printf(
		"Usage:\n"
#ifdef _WIN32 // Windows
		"    idCrypt.exe [-decrypt | -encrypt] <file-path> <internal-file-path>\n"
#else // Linux
		"    ./idCrypt [-decrypt | -encrypt] <file-path> <internal-file-path>\n"
#endif
		"\n"
		"Example:\n"
#ifdef _WIN32 // Windows
		"    idCrypt.exe \"D:\\english.bfile\" \"strings/english.lang\"\n"
#else // Linux
		"    ./idCrypt \"./english.bfile\" \"strings/english.lang\"\n"
#endif
		"\n"
		"If a .dec file is supplied, it'll be encrypted to <file-path>.bfile\n"
		"Otherwise the file will be decrypted to <file-path>.dec\n"
		"This can be overriden with the -decrypt or -encrypt options\n"
		"\n"
		"You _must_ use the correct internal filepath for decryption to succeed!\n"
	);
}

int main(int argc, char *argv[])
{
	printf("idCrypt originally by emoose, v0.2+ by Zwip-Zwap Zapony\n"
		"https://github.com/ZwipZwapZapony/DOOMModLoader\n\n");

	signed char decrypt = -1;
	char* filePath = NULL;
	char* internalPath = NULL;

	for (int i = 1; i < argc; i++)
	{
		if (argv[i][0] == '-' || argv[i][0] == '/') // Check for command-line options
		{
			char option[16]; // Must be longer than the longest option's name plus null byte
			if (argv[i][0] == '-' && argv[i][1] == '-')
				strlcpy(option, argv[i] + 2, sizeof option); // Skip the "--" prefix
			else
				strlcpy(option, argv[i] + 1, sizeof option); // Skip the "-" or "/" prefix
			for (int c = 0; option[c]; c++) // Make it lowercase, for case-insensitivity
				option[c] = tolower(option[c]);

			if (!strcmp(option, "decrypt"))
			{
				decrypt = 1;
				continue;
			}
			else if (!strcmp(option, "encrypt"))
			{
				decrypt = 0;
				continue;
			}
			else if (!strcmp(option, "help"))
			{
				PrintUsage();
				return 0;
			}
		}

		// If it wasn't recognised above, it should be the file-path or internal-path
		if (filePath == NULL)
			filePath = argv[i];
		else if (internalPath == NULL)
			internalPath = argv[i];
		else // Too many paths/unrecognised options?
		{
			PrintUsage();
			return 1;
		}
	}

	if (filePath == NULL || internalPath == NULL) // Didn't specify both paths
	{
		PrintUsage();
		return 1;
	}

	if (decrypt == -1) // "-decrypt" and "-encrypt" were not specified, so auto-detect it
	{
		decrypt = 1; // Default to decryption mode

		// If the file extension is ".dec" (case-insensitive), switch to encryption mode
		char* dot = strrchr(filePath, '.');
		if (dot)
		{
			char lowerExt[sizeof("dec") + 1]; // One byte more than "dec\0"
			strlcpy(lowerExt, dot + 1, sizeof lowerExt);
			for (int i = 0; lowerExt[i]; i++)
				lowerExt[i] = tolower(lowerExt[i]);

			if (!strcmp(lowerExt, "dec"))
				decrypt = 0;
		}
	}

	char destPath[256];
	sprintf_s(destPath, 256, "%s.%s", filePath, decrypt ? "dec" : "bfile");

	FILE* file;
	int res = fopen_s(&file, filePath, "rb");
	if (res != 0)
	{
		printf("Failed to open %s for reading (error %d)\n", filePath, res);
		return 2;
	}

	fseek(file, 0, SEEK_END);
	long size = ftell(file);
	fseek(file, 0, SEEK_SET);

	BYTE* fileData = (BYTE*)malloc(size);
	fread(fileData, 1, size, file);
	fclose(file);

	BYTE fileSalt[0xC];
	if (decrypt)
		memcpy(fileSalt, fileData, 0xC);
	else
		BCryptGenRandom(NULL, (PBYTE)fileSalt, 0xC, BCRYPT_USE_SYSTEM_PREFERRED_RNG);

	BYTE encKey[0x20];
	res = hash_data((void*)fileSalt, 0xC, (void*)keyDeriveStatic, 0xA, internalPath, strlen(internalPath), NULL, 0, encKey);
	if (!NT_SUCCESS(res))
	{
		printf("Failed to derive encryption key (error 0x%x)\n", res);
		return 3;
	}

	BYTE fileIV[0x10];
	BYTE fileIV_backup[0x10];
	if (decrypt)
		memcpy(fileIV, fileData + 0xC, 0x10);
	else
		BCryptGenRandom(NULL, (PBYTE)fileIV, 0x10, BCRYPT_USE_SYSTEM_PREFERRED_RNG);

	memcpy(fileIV_backup, fileIV, 0x10); // make a backup of IV because BCrypt can overwrite it (and make you waste an hour debugging in the process...)

	BYTE* fileText = fileData;
	long fileTextSize = size;

	BYTE hmac[0x20];
	if (decrypt) // change fileText pointer + verify HMAC if we're decrypting
	{
		fileText = fileData + 0x1C;
		fileTextSize = size - 0x1C - 0x20;

		BYTE* fileHmac = fileData + (size - 0x20);

		res = hash_data((void*)fileSalt, 0xC, (void*)fileIV, 0x10, (void*)fileText, fileTextSize, encKey, 0x20, hmac);
		if (!NT_SUCCESS(res))
		{
			printf("Failed to create HMAC hash of ciphertext (error 0x%x)\n", res);
			return 4;
		}

		if (memcmp(hmac, fileHmac, 0x20))
			printf("Warning: HMAC hash check failed, decrypted data might not be valid!\n");
	}

	// call crypt_data with NULL buffer to get the buffer size
	ULONG cryptedTextSize = 0;
	res = crypt_data(decrypt, fileText, fileTextSize, encKey, 0x10, fileIV, 0x10, 0, &cryptedTextSize);
	if (!NT_SUCCESS(res))
	{
		printf("Failed to %s data (error 0x%x)\n", decrypt ? "decrypt" : "encrypt", res);
		printf("Did you use the correct internal file name?\n");
		return 5;
	}

	memcpy(fileIV, fileIV_backup, 0x10); // BCryptEncrypt overwrites the IV, so restore it from backup

	// now allocate that buffer and call crypt_data for realsies
	BYTE* cryptedText = (BYTE*)malloc(cryptedTextSize);
	res = crypt_data(decrypt, fileText, fileTextSize, encKey, 0x10, fileIV, 0x10, cryptedText, &cryptedTextSize);
	if (!NT_SUCCESS(res))
	{
		printf("Failed to %s data (error 0x%x)\n", decrypt ? "decrypt" : "encrypt", res);
		printf("Did you use the correct internal file name?\n");
		return 5;
	}

	memcpy(fileIV, fileIV_backup, 0x10); // BCryptEncrypt overwrites the IV, so restore it from backup

	free(fileData);

	res = fopen_s(&file, destPath, "wb+");
	if (res != 0)
	{
		printf("Failed to open %s for writing (error %d)\n", destPath, res);
		return 6;
	}

	if(decrypt)
		fwrite(cryptedText, 1, cryptedTextSize, file);
	else
	{
		fwrite(fileSalt, 1, 0xC, file);
		fwrite(fileIV, 1, 0x10, file);
		fwrite(cryptedText, 1, cryptedTextSize, file);

		res = hash_data((void*)fileSalt, 0xC, (void*)fileIV, 0x10, (void*)cryptedText, cryptedTextSize, encKey, 0x20, hmac);
		if (!NT_SUCCESS(res))
		{
			printf("Failed to create HMAC hash of ciphertext (error 0x%x)\n", res);
			return 7;
		}
		fwrite(hmac, 1, 0x20, file);
	}

	fclose(file);

	//free(cryptedText);
	// ^ causes an error, wtf?

	printf("%s succeeded! Wrote to %s\n", decrypt ? "Decryption" : "Encryption", destPath);
	return 0;
}
