import unittest
from urllib import request, response
import requests
import json

class TestRegisterUsername(unittest.TestCase):
    url = 'http://localhost:8080/register'
    expectedInvalidResult = {"error" : "Username invalid format"}
    expectedValidResult = {"error": "Password invalid format"}

    def createBodyData(self, username):
        data = { 
            "username": username,
            "password": "1234", 
            "email": "testtest@test.com",
            "displayName": "TestTest"
        }
        return json.dumps(data)

    def getResponse(self, data):
        #Make a POST request to the url and send the data
        response = requests.post(self.url, data=data, timeout=10)
        return json.loads(response.text)

    #========================/INVALID USERNAMES TESTS\========================

    #Empty username
    def test_username_empty(self):
        '''
            Test if username empty
        '''
        data = self.createBodyData("")
        response = self.getResponse(data)
        self.assertEqual(response, self.expectedInvalidResult)

    #Username is less than 6 characters
    def test_username_too_short(self):
        '''
            Test username too short
        '''
        data = self.createBodyData("luca")
        response = self.getResponse(data)
        self.assertEqual(response, self.expectedInvalidResult)

    #Username less than 6 characters but starts with uppercase
    def test_username_starts_uppercase(self):
        '''
            Test username too short (start uppercase)
        '''
        data = self.createBodyData("Luca")
        response = self.getResponse(data)
        self.assertEqual(response, self.expectedInvalidResult)

    #Username less than 6 characters, all characters are uppercase
    def test_username_short_uppercase(self):
        '''
           Test username short (all uppercase)
        '''
        data = self.createBodyData("LUCA")
        response = self.getResponse(data)
        self.assertEqual(response, self.expectedInvalidResult)

    #Username longer than 20 characters
    def test_username_long(self):
        '''
            Test username too long
        '''
        data = self.createBodyData("L" * 21)
        response = self.getResponse(data)
        self.assertEqual(response, self.expectedInvalidResult)

    #Username is a number
    def test_username_number(self):
        '''
            Test username is a number (short)
        ''' 
        data = self.createBodyData("1234")
        response = self.getResponse(data)
        self.assertEqual(response, self.expectedInvalidResult)

    #Username is a number longer than 20 characters
    def test_username_number_long(self):
        '''
            Test username is a number (long)
        '''
        data = self.createBodyData('1' * 21)
        response = self.getResponse(data)
        self.assertEqual(response, self.expectedInvalidResult)

    #Username has special characters other than _
    def test_username_special_chars(self):
        '''
            Test username with special chars
        '''
        data = self.createBodyData('test../\*%$test')
        response = self.getResponse(data)
        self.assertEqual(response, self.expectedInvalidResult)

    #Username is large (2048 characters)
    def test_username_large(self):
        '''
            Test username large (2048 characters)
        '''
        data = self.createBodyData('test' * 512)
        response = self.getResponse(data)
        self.assertEqual(response, self.expectedInvalidResult)

    #Username is very large (8192)
    def test_username_very_large(self):
        '''
            Test username very large (8192 characters)
        '''
        data = self.createBodyData('test' * 2048)
        response = self.getResponse(data)
        self.assertEqual(response, self.expectedInvalidResult)

    #========================/END INVALID USERNAMES TESTS\========================


    #========================/VALID USERNAMES TESTS\========================
    #Test username 10 characters long
    def test_username_10_characters_lower(self):
        '''
            Test username 10 characters (lower)
        '''
        data = self.createBodyData('testtestte')
        response = self.getResponse(data)
        self.assertEqual(response, self.expectedValidResult)

    #Test username 15 characters (all lower)
    def test_username_15_characters_lower(self):
        '''
            Test username 15 characters (lower)
        '''
        data = self.createBodyData('testtesttesttes')
        response = self.getResponse(data)
        self.assertEqual(response, self.expectedValidResult)

    #Test username 20 characters (all lower)
    def test_username_20_characters_lower(self):
        '''
            Test username 20 characters (all lower)
        '''
        data = self.createBodyData('test' * 5)
        response = self.getResponse(data)
        self.assertEqual(response, self.expectedValidResult)

    
    #Test username both lower and upper
    def test_username_upper_and_lower(self):
        '''
            Test username upper and lower characters
        '''
        data = self.createBodyData('TesT' * 3)
        response = self.getResponse(data)
        self.assertEqual(response, self.expectedValidResult)

    #Test username all upper
    def test_username_all_upper(self):
        '''
            Test username (all upper)
        '''
        data = self.createBodyData('TEST' * 4)
        response = self.getResponse(data)
        self.assertEqual(response, self.expectedValidResult)

    #Test username lower and digit
    def test_username_lower_digit(self):
        '''
            Test username lower with digit
        '''
        data = self.createBodyData('testtest1')
        response = self.getResponse(data)
        self.assertEqual(response, self.expectedValidResult)

    #Test username lower and digits
    def test_username_lower_digits(self):
        '''
            Test username lower with digits
        '''
        data = self.createBodyData('testest1234')
        response = self.getResponse(data)
        self.assertEqual(response, self.expectedValidResult)

    #Test username upper and digit
    def test_username_upper_digit(self):
        '''
            Test username upper with digit
        '''
        data = self.createBodyData('TESTTEST1')
        response = self.getResponse(data)
        self.assertEqual(response, self.expectedValidResult)

    #Test username upper and digits
    def test_username_upper_digits(self):
        '''
            Test username upper with digits
        '''
        data = self.createBodyData('TESTEST19786')
        response = self.getResponse(data)
        self.assertEqual(response, self.expectedValidResult)

    #Test username lower, upper and digit
    def test_username_lower_upper_digit(self):
        '''
            Test username lower, upper, digit
        '''
        data = self.createBodyData('TeStTesT6')
        response = self.getResponse(data)
        self.assertEqual(response, self.expectedValidResult)

    #Test username lower, upper, digits
    def test_username_lower_upper_digits(self):
        '''
            Test username lower, upper, digits
        '''
        data = self.createBodyData('TeSTteST1876')
        response = self.getResponse(data)
        self.assertEqual(response, self.expectedValidResult)

    #Test username lower, underscore
    def test_username_lower_underscore(self):
        '''
            Test username lower with underscore
        '''
        data = self.createBodyData('test_test_test')
        response = self.getResponse(data)
        self.assertEqual(response, self.expectedValidResult)

    #Test username upper, underscore
    def test_username_upper_underscore(self):
        '''
            Test username upper with underscore
        '''
        data = self.createBodyData('TEST_TEST_TEST')
        response = self.getResponse(data)
        self.assertEqual(response, self.expectedValidResult)

    #Test username upper, lower, underscore
    def test_usernamte_upper_lower_underscore(self):
        '''
            Test username upper, lower with underscore
        '''
        data = self.createBodyData('TesT_TeST')
        response = self.getResponse(data)
        self.assertEqual(response, self.expectedValidResult)

    #Test multiple underscore
    def test_multiple_underscore(self):
        '''
            Test multiple underscore
        '''
        data = self.createBodyData('te_st_te_st')
        response = self.getResponse(data)
        self.assertEqual(response, self.expectedValidResult)

    #Test all valid characters
    def test_all_valid_characters(self):
        '''
            Test all valid characters
        '''
        data = self.createBodyData('Te_St_1_Te_1_s9012t')
        response = self.getResponse(data)
        self.assertEqual(response, self.expectedValidResult)
    

if __name__ == '__main__':
    unittest.main()