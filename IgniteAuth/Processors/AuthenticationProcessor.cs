using System;
using System.Collections.Generic;
using System.Text;

namespace IgniteAuth.Processors
{
    internal class AuthenticationProcessor
    {

        private bool HashVerify(string a)
        {
            a = a + "HashedString";

            if (a.Length > 0)
            {
                return true;
            }
            return false;

        }

        // This page tells How User Entered Data is Handled, Hashed and stored
        public AuthenticationProcessor(string Username, string Password) {


            Console.WriteLine($"Trying to Login for User {Username} ");

            if(!HashVerify(Password)) {
                Console.WriteLine("Invalid Credentials");

            }

        }

    }


}
