﻿#r @"packages\FsPickler\lib\net45\FsPickler.dll"
#r @"packages\Suave\lib\net40\Suave.dll"
#r @"packages\Suave.Experimental\lib\net40\Suave.Experimental.dll"

open Suave.Form
open System.Net.Mail

type Album = {
    ArtistId : decimal
    GenreId : decimal
    Title : string
    Price : decimal
    ArtUrl : string
}

let album : Form<Album> =
    Form (
        [
            TextProp ((fun f -> <@ f.Title @>), [ maxLength 100 ])
            TextProp ((fun f -> <@ f.ArtUrl @>), [ maxLength 100 ])
            DecimalProp ((fun f -> <@ f.Price @>), [ min 0.01M; max 100.0M; step 0.01M ])
        ],
        []
    )

type Logon = {
    Username : string
    Password : Password
}

let logon : Form<Logon> = Form ([], [])

type Register = {
    Username : string
    Email : MailAddress
    Password : Password
    ConfirmPassword : Password
}

let pattern = @"(\w){6,20}"

let passwordsMatch =
    (fun f -> f.Password = f.ConfirmPassword), "Passwords must match"

let register : Form<Register> =
    Form (
        [ TextProp ((fun f -> <@ f.Username @>), [ maxLength 30 ])
          PasswordProp ((fun f -> <@ f.Password @>), [ passwordRegex pattern ])
          PasswordProp ((fun f -> <@ f.ConfirmPassword @>), [ passwordRegex pattern ]) ],
        [ passwordsMatch ]
    )

type Checkout = {
    FirstName : string
    LastName : string
    Address : string
    PromoCode : string option
}

let checkout : Form<Checkout> = Form ([], [])
