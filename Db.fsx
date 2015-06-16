#r @"D:\NugetLibs\SQLProvider.0.0.9-alpha\lib\net40\FSharp.Data.SqlProvider.dll"

open System
open FSharp.Data.Sql
    
type Sql =
    SqlDataProvider<"Data Source=(localdb)\\v11.0;Initial Catalog=SuaveMusicStore;Integrated Security=True;Pooling=False",
                    DatabaseVendor = Common.DatabaseProviderTypes.MSSQLSERVER>
type DbContext = Sql.dataContext
type Artist = DbContext.``[dbo].[Artists]Entity``
type Album = DbContext.``[dbo].[Albums]Entity``
type Genre = DbContext.``[dbo].[Genres]Entity``
type AlbumDetails = DbContext.``[dbo].[AlbumDetails]Entity``
type User = DbContext.``[dbo].[Users]Entity``
type Cart = DbContext.``[dbo].[Carts]Entity``
type CartDetails = DbContext.``[dbo].[CartDetails]Entity``
type BestSeller = DbContext.``[dbo].[BestSellers]Entity``

let firstOrNone s = s |> Seq.tryFind (fun _ -> true)

let getArtists (ctx : DbContext) : Artist list =
    ctx.``[dbo].[Artists]`` |> Seq.toList |> List.sortBy (fun a -> a.Name)

let getGenres (ctx : DbContext) : Genre list =
    ctx.``[dbo].[Genres]`` |> Seq.toList |> List.sortBy (fun g -> g.Name)

let getAlbumsForGenre genreName (ctx : DbContext) : Album list =
    query {
        for album in ctx.``[dbo].[Albums]`` do
        join genre in ctx.``[dbo].[Genres]`` on (album.GenreId = genre.GenreId)
        where (genre.Name = genreName)
        sortBy (album.Title)
        select album
    }
    |> Seq.toList
let getAlbum id (ctx : DbContext) : Album option =
    query {
        for album in ctx.``[dbo].[Albums]`` do
        where (album.AlbumId = id)
        select album
    }
    |> firstOrNone
let getAlbumDetails id (ctx : DbContext) : AlbumDetails option =
    query {
        for album in ctx.``[dbo].[AlbumDetails]`` do
        where (album.AlbumId = id)
        select album
    }
    |> firstOrNone
let getAlbumsDetails (ctx : DbContext) : AlbumDetails list =
    ctx.``[dbo].[AlbumDetails]`` |> Seq.toList |> List.sortBy (fun ad -> ad.Artist, ad.Title)
let createAlbum (artistId, genreId, price, title) (ctx : DbContext) =
    ctx.``[dbo].[Albums]``.Create(artistId, genreId, price, title) |> ignore
    ctx.SubmitUpdates()
let updateAlbum (album : Album) (artistId, genreId, price, title) (ctx : DbContext) =
    album.ArtistId <- artistId
    album.GenreId <- genreId
    album.Price <- price
    album.Title <- title
    ctx.SubmitUpdates()
let deleteAlbum (album : Album) (ctx : DbContext) =
    album.Delete()
    ctx.SubmitUpdates()

let validateUser (username, password) (ctx : DbContext) : User option =
    query {
        for user in ctx.``[dbo].[Users]`` do
        where (user.UserName = username && user.Password = password)
        select user
    }
    |> firstOrNone
let getUser username (ctx : DbContext) : User option =
    query {
        for user in ctx.``[dbo].[Users]`` do
        where (user.UserName = username)
        select user
    }
    |> firstOrNone
let newUser (username, password, email) (ctx : DbContext) =
    let user = ctx.``[dbo].[Users]``.Create(email, password, "user", username)
    ctx.SubmitUpdates()
    user

let getCarts cartId (ctx : DbContext) : Cart list =
    query {
        for cart in ctx.``[dbo].[Carts]`` do
        where (cart.CartId = cartId)
        select cart
    }
    |> Seq.toList
let getCart cartId albumId (ctx : DbContext) : Cart option =
    query {
        for cart in ctx.``[dbo].[Carts]`` do
        where (cart.CartId = cartId && cart.AlbumId = albumId)
        select cart
    }
    |> firstOrNone
let addToCart cartId albumId (ctx : DbContext) =
    match getCart cartId albumId ctx with
    | Some cart ->
        cart.Count <- cart.Count + 1
    | None ->
        ctx.``[dbo].[Carts]``.Create(albumId, cartId, 1, DateTime.UtcNow) |> ignore
    ctx.SubmitUpdates()
let getCartDetails cartId (ctx : DbContext) : CartDetails list =
    query {
        for cart in ctx.``[dbo].[CartDetails]`` do
        where (cart.CartId = cartId)
        select cart
    }
    |> Seq.toList
let removeFromCart (cart : Cart) albumId (ctx : DbContext) =
    cart.Count <- cart.Count - 1
    if cart.Count = 0 then cart.Delete()
    ctx.SubmitUpdates()
let upgradeCarts (cartId : string, username : string) (ctx : DbContext) =
    for cart in getCarts cartId ctx do
        match getCart username cart.AlbumId ctx with
        | Some existing ->
            existing.Count <- existing.Count + cart.Count
            cart.Delete()
        | None ->
            cart.CartId <- username
    ctx.SubmitUpdates()

let placeOrder (username : string) (ctx : DbContext) =
    let carts = getCartDetails username ctx
    let total = carts |> List.sumBy (fun c -> decimal c.Count * c.Price)
    let order = ctx.``[dbo].[Orders]``.Create(DateTime.UtcNow, total)
    order.Username <- username
    ctx.SubmitUpdates()
    for cart in carts do
        let orderDetails = ctx.``[dbo].[OrderDetails]``.Create(cart.AlbumId, order.OrderId, cart.Count, cart.Price)
        getCart cart.CartId cart.AlbumId ctx
        |> Option.iter (fun cart -> cart.Delete())
    ctx.SubmitUpdates()

let getBestSellers (ctx : DbContext) : BestSeller list =
    ctx.``[dbo].[BestSellers]`` |> Seq.toList

let getContext () = Sql.GetDataContext()
